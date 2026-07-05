import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import type { HexMap, HexTile, Vec3 } from '../../api';
import {
  BIOME_COLORS,
  CIV_FILL_COLORS,
  canClaim,
  parseCssColor,
  tileExtrusionHeight,
  tileKey,
} from './hexMapModel';
import { surfacePoint } from './globeProjection';

export type ThreeHexMapCallbacks = {
  onTileSelect: (tile: HexTile) => void;
  onTileClaim: (tile: HexTile) => void;
  onHoverChange: (tile: HexTile | null) => void;
};

export type CameraState = {
  position: [number, number, number];
  target: [number, number, number];
};

export type ThreeHexMapHandle = {
  setSelected: (tile: HexTile | null) => void;
  syncMap: (map: HexMap, myCivilizationId: string | null, disabled: boolean, canInteract: boolean) => void;
  zoomBy: (factor: number) => void;
  fitToView: () => void;
  getCameraState: () => CameraState;
  destroy: () => void;
};

type TileRecord = {
  tile: HexTile;
  mesh: THREE.Mesh;
  material: THREE.MeshStandardMaterial;
  surface: THREE.Vector3;
  normal: THREE.Vector3;
  extrude: number;
  overlay: THREE.Mesh | null;
  capital: THREE.Mesh | null;
};

const INITIAL_VIEW_ZOOM = 2.1;
const GLOBE_CENTER = new THREE.Vector3(0, 0, 0);
const LIGHTEN = new THREE.Color(0xffffff);
const TILE_BORDER_COLOR = 0x0a1520;

const SELECT_EMISSIVE = 0x4cd7f6;
const HOVER_EMISSIVE = 0x2a8a9e;

function toThreeColor(css: string): THREE.Color {
  return new THREE.Color(parseCssColor(css).color);
}

function lightenBiomeColor(biome: string): THREE.Color {
  const color = toThreeColor(BIOME_COLORS[biome] ?? '#64748b');
  if (biome === 'Ocean') {
    color.lerp(LIGHTEN, 0.08);
    return color;
  }
  color.lerp(LIGHTEN, 0.42);
  return color;
}

function tileCenter(tile: HexTile): THREE.Vector3 {
  return new THREE.Vector3(tile.centerX, tile.centerY, tile.centerZ);
}

function tileNormal(tile: HexTile): THREE.Vector3 {
  return new THREE.Vector3(tile.normalX, tile.normalY, tile.normalZ).normalize();
}

function scalePolygon(polygon: Vec3[], center: THREE.Vector3, scale: number): Vec3[] {
  return polygon.map((v) => {
    const p = new THREE.Vector3(v.x, v.y, v.z);
    p.sub(center).multiplyScalar(scale).add(center);
    return { x: p.x, y: p.y, z: p.z };
  });
}

/** Rigid prism: base ring uses exact backend vertices; extrude along tile normal. */
function createWorldSpacePrism(
  polygon: Vec3[],
  normal: THREE.Vector3,
  height: number,
): THREE.BufferGeometry {
  const n = polygon.length;
  if (n < 3) return new THREE.BufferGeometry();

  const norm = normal.clone().normalize();
  const bottom = polygon.map((v) => new THREE.Vector3(v.x, v.y, v.z));
  const top = bottom.map((v) => v.clone().addScaledVector(norm, height));

  const positions: number[] = [];
  const indices: number[] = [];
  const addVertex = (v: THREE.Vector3) => {
    positions.push(v.x, v.y, v.z);
    return positions.length / 3 - 1;
  };

  const bottomIdx = bottom.map(addVertex);
  const topIdx = top.map(addVertex);

  for (let i = 0; i < n; i++) {
    const j = (i + 1) % n;
    const a = bottomIdx[i];
    const b = bottomIdx[j];
    const c = topIdx[j];
    const d = topIdx[i];
    indices.push(a, b, c, a, c, d);
  }

  const topCenter = top.reduce((sum, v) => sum.add(v), new THREE.Vector3()).divideScalar(n);
  const topCenterIdx = addVertex(topCenter);
  for (let i = 0; i < n; i++) {
    const j = (i + 1) % n;
    indices.push(topCenterIdx, topIdx[i], topIdx[j]);
  }

  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
  geo.setIndex(indices);
  geo.computeVertexNormals();
  return geo;
}

function createTopRingLine(
  polygon: Vec3[],
  normal: THREE.Vector3,
  height: number,
): THREE.BufferGeometry {
  const norm = normal.clone().normalize();
  const points = polygon.map((v) =>
    new THREE.Vector3(v.x, v.y, v.z).addScaledVector(norm, height),
  );
  points.push(points[0].clone());
  return new THREE.BufferGeometry().setFromPoints(points);
}

function waitForHostSize(host: HTMLElement): Promise<void> {
  return new Promise((resolve) => {
    const ready = () => host.clientWidth >= 2 && host.clientHeight >= 2;
    if (ready()) {
      resolve();
      return;
    }
    const observer = new ResizeObserver(() => {
      if (ready()) {
        observer.disconnect();
        resolve();
      }
    });
    observer.observe(host);
    requestAnimationFrame(() => {
      if (ready()) {
        observer.disconnect();
        resolve();
      }
    });
  });
}

function focusOnGlobe(tiles: HexTile[], myCivilizationId: string | null): THREE.Vector3 {
  const owned = tiles.filter(
    (t) => myCivilizationId && t.controllingCivilizationId === myCivilizationId,
  );
  const sample = owned.length > 0 ? owned : tiles.filter((t) => t.biome !== 'Ocean').slice(0, 40);
  if (sample.length === 0) return new THREE.Vector3(100, 0, 0);

  const sum = new THREE.Vector3();
  for (const tile of sample) sum.add(tileCenter(tile));
  return sum.divideScalar(sample.length);
}

export async function createThreeHexMap(
  host: HTMLElement,
  map: HexMap,
  hexSize: number,
  myCivilizationId: string | null,
  disabled: boolean,
  canInteract: boolean,
  callbacks: ThreeHexMapCallbacks,
  restoreCamera?: CameraState | null,
): Promise<ThreeHexMapHandle> {
  await waitForHostSize(host);

  const planetRadius = map.planetRadius > 0 ? map.planetRadius : 100;

  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0a1e32);
  scene.fog = new THREE.Fog(0x0a1e32, planetRadius * 2, planetRadius * 5.5);

  const camera = new THREE.PerspectiveCamera(48, 1, 0.1, 5000);
  const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setSize(host.clientWidth, host.clientHeight, false);
  renderer.domElement.classList.add('hex-map-canvas');
  renderer.domElement.setAttribute('role', 'img');
  renderer.domElement.setAttribute('aria-label', 'Territory map');
  host.appendChild(renderer.domElement);

  scene.add(new THREE.AmbientLight(0xc8d8f0, 0.78));
  const sun = new THREE.DirectionalLight(0xfff8ee, 1.45);
  sun.position.set(planetRadius * 1.8, planetRadius * 1.2, -planetRadius);
  scene.add(sun);
  const fill = new THREE.DirectionalLight(0x9ee8ff, 0.52);
  fill.position.set(-planetRadius * 1.4, planetRadius * 0.4, planetRadius);
  scene.add(fill);
  const rim = new THREE.DirectionalLight(0xd4e4fa, 0.35);
  rim.position.set(0, -planetRadius, planetRadius * 1.2);
  scene.add(rim);

  const terrain = new THREE.Group();
  const markers = new THREE.Group();
  const highlights = new THREE.Group();
  scene.add(terrain);
  scene.add(markers);
  scene.add(highlights);

  const tileHeight = tileExtrusionHeight(hexSize);

  const disposables: THREE.BufferGeometry[] = [];
  const materials: THREE.Material[] = [];
  const geometryCache = new Map<string, THREE.BufferGeometry>();
  const borderCache = new Map<string, THREE.BufferGeometry>();
  const tileBorderMaterial = new THREE.LineBasicMaterial({
    color: TILE_BORDER_COLOR,
    transparent: true,
    opacity: 0.9,
  });
  materials.push(tileBorderMaterial);

  const getPrismGeometry = (tile: HexTile, height: number): THREE.BufferGeometry => {
    const key = tile.id;
    let geometry = geometryCache.get(key);
    if (!geometry) {
      geometry = createWorldSpacePrism(tile.polygonVertices, tileNormal(tile), height);
      geometryCache.set(key, geometry);
      disposables.push(geometry);
    }
    return geometry;
  };

  const getBorderGeometry = (tile: HexTile, height: number): THREE.BufferGeometry => {
    let geometry = borderCache.get(tile.id);
    if (!geometry) {
      geometry = createTopRingLine(tile.polygonVertices, tileNormal(tile), height);
      borderCache.set(tile.id, geometry);
      disposables.push(geometry);
    }
    return geometry;
  };

  const oceanShell = new THREE.Mesh(
    new THREE.SphereGeometry(planetRadius * 0.985, 72, 48),
    new THREE.MeshStandardMaterial({ color: toThreeColor(BIOME_COLORS.Ocean), roughness: 0.88, metalness: 0.02 }),
  );
  terrain.add(oceanShell);

  const atmosphere = new THREE.Mesh(
    new THREE.SphereGeometry(planetRadius * 1.045, 48, 32),
    new THREE.MeshStandardMaterial({
      color: 0x4cd7f6,
      transparent: true,
      opacity: 0.1,
      roughness: 1,
      depthWrite: false,
      side: THREE.BackSide,
    }),
  );
  terrain.add(atmosphere);

  const tileRecords = new Map<string, TileRecord>();
  const pickables: THREE.Mesh[] = [];
  let liveMap = map;
  let liveCivId = myCivilizationId;
  let liveDisabled = disabled;
  let liveCanInteract = canInteract;

  const getClaimable = (tile: HexTile) =>
    liveCanInteract && !liveDisabled && canClaim(tile, liveMap, liveCivId);

  const removeMarker = (mesh: THREE.Mesh | null) => {
    if (!mesh) return;
    markers.remove(mesh);
    mesh.geometry.dispose();
    (mesh.material as THREE.Material).dispose();
  };

  const buildOwnerOverlay = (
    tile: HexTile,
    surface: THREE.Vector3,
    normal: THREE.Vector3,
    extrude: number,
  ): THREE.Mesh | null => {
    const ownerFill = tile.controllingCivilizationId
      ? CIV_FILL_COLORS[tile.controllingCivilizationId]
      : null;
    if (!ownerFill) return null;

    const capH = hexSize * 0.06;
    const shrunk = scalePolygon(tile.polygonVertices, surface, 0.76);
    const liftedPoly = shrunk.map((v) => ({
      x: v.x + normal.x * extrude,
      y: v.y + normal.y * extrude,
      z: v.z + normal.z * extrude,
    }));
    const overlayGeo = createWorldSpacePrism(liftedPoly, normal, capH);
    disposables.push(overlayGeo);
    const overlayMat = new THREE.MeshStandardMaterial({
      color: toThreeColor(ownerFill),
      transparent: true,
      opacity: parseCssColor(ownerFill).alpha,
      roughness: 0.6,
      flatShading: true,
    });
    materials.push(overlayMat);
    const overlay = new THREE.Mesh(overlayGeo, overlayMat);
    markers.add(overlay);
    return overlay;
  };

  const buildCapital = (
    tile: HexTile,
    surface: THREE.Vector3,
    normal: THREE.Vector3,
    extrude: number,
  ): THREE.Mesh | null => {
    if (!tile.isCapital) return null;
    const capGeo = new THREE.SphereGeometry(hexSize * 0.14, 12, 10);
    disposables.push(capGeo);
    const capMat = new THREE.MeshStandardMaterial({
      color: 0xec6a06,
      emissive: 0x6a2800,
      emissiveIntensity: 0.35,
      roughness: 0.45,
    });
    materials.push(capMat);
    const cap = new THREE.Mesh(capGeo, capMat);
    cap.position.copy(surfacePoint(surface, normal, extrude + hexSize * 0.12));
    markers.add(cap);
    return cap;
  };

  const tilesToRender = map.tiles
    .filter((t) => t.polygonVertices?.length)
    .sort((a, b) => {
      if (a.biome === 'Ocean' && b.biome !== 'Ocean') return -1;
      if (a.biome !== 'Ocean' && b.biome === 'Ocean') return 1;
      return 0;
    });

  for (const tile of tilesToRender) {
    const isOcean = tile.biome === 'Ocean';
    const surface = tileCenter(tile);
    const normal = tileNormal(tile);
    const extrude = tileHeight;
    const geometry = getPrismGeometry(tile, extrude);

    const material = new THREE.MeshStandardMaterial({
      color: lightenBiomeColor(tile.biome),
      roughness: isOcean ? 0.78 : 0.62,
      metalness: isOcean ? 0.04 : 0.02,
      flatShading: true,
    });
    materials.push(material);

    const mesh = new THREE.Mesh(geometry, material);
    if (!isOcean) {
      const border = new THREE.Line(getBorderGeometry(tile, extrude), tileBorderMaterial);
      mesh.add(border);
    }

    const key = tileKey(tile);
    mesh.userData = { tile, key };
    terrain.add(mesh);

    if (!isOcean) {
      pickables.push(mesh);
      const overlay = buildOwnerOverlay(tile, surface, normal, extrude);
      const capital = buildCapital(tile, surface, normal, extrude);
      tileRecords.set(key, {
        tile,
        mesh,
        material,
        surface,
        normal,
        extrude,
        overlay,
        capital,
      });
    }
  }

  const selectionRing = new THREE.Line(
    new THREE.BufferGeometry(),
    new THREE.LineBasicMaterial({
      color: 0x4cd7f6,
      transparent: true,
      opacity: 0.95,
    }),
  );
  selectionRing.visible = false;
  highlights.add(selectionRing);
  materials.push(selectionRing.material as THREE.Material);

  const viewFocus = focusOnGlobe(map.tiles, myCivilizationId);

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.06;
  controls.minDistance = planetRadius * 0.5;
  controls.maxDistance = planetRadius * 4.2;
  controls.screenSpacePanning = false;
  controls.enablePan = true;
  controls.minPolarAngle = 0.08;
  controls.maxPolarAngle = Math.PI - 0.08;
  controls.mouseButtons = {
    LEFT: THREE.MOUSE.ROTATE,
    MIDDLE: THREE.MOUSE.DOLLY,
    RIGHT: THREE.MOUSE.PAN,
  };

  let selectedKey: string | null = null;
  let hoveredKey: string | null = null;

  const clearEmissive = (record: TileRecord) => {
    record.material.emissive.setHex(0x000000);
    record.material.emissiveIntensity = 0;
  };

  const applyHover = (key: string | null) => {
    if (hoveredKey && hoveredKey !== selectedKey) {
      const prev = tileRecords.get(hoveredKey);
      if (prev) clearEmissive(prev);
    }
    hoveredKey = key;
    if (key && key !== selectedKey) {
      const record = tileRecords.get(key);
      if (record) {
        record.material.emissive.setHex(HOVER_EMISSIVE);
        record.material.emissiveIntensity = 0.35;
      }
    }
  };

  const applySelectionRing = (key: string | null) => {
    if (!key) {
      selectionRing.visible = false;
      return;
    }
    const record = tileRecords.get(key);
    if (!record) {
      selectionRing.visible = false;
      return;
    }
    selectionRing.visible = true;
    const ringGeo = createTopRingLine(
      scalePolygon(record.tile.polygonVertices, record.surface, 1.02),
      record.normal,
      tileHeight + hexSize * 0.01,
    );
    selectionRing.geometry.dispose();
    selectionRing.geometry = ringGeo;
    disposables.push(ringGeo);
  };

  const setSelectedKey = (key: string | null) => {
    if (selectedKey) {
      const prev = tileRecords.get(selectedKey);
      if (prev) clearEmissive(prev);
    }
    selectedKey = key;
    if (key) {
      const record = tileRecords.get(key);
      if (record) {
        record.material.emissive.setHex(SELECT_EMISSIVE);
        record.material.emissiveIntensity = 0.5;
      }
    }
    applySelectionRing(key);
    applyHover(hoveredKey);
  };

  const fitToView = (zoomMultiplier = INITIAL_VIEW_ZOOM) => {
    controls.target.copy(GLOBE_CENTER);
    const distance = (planetRadius * 2.35) / zoomMultiplier;
    const lookDir =
      viewFocus.lengthSq() > 1e-6
        ? viewFocus.clone().normalize()
        : new THREE.Vector3(0.25, 0.35, 1).normalize();
    camera.position.copy(lookDir).multiplyScalar(distance);
    controls.update();
  };

  const applyCameraState = (state: CameraState) => {
    camera.position.set(state.position[0], state.position[1], state.position[2]);
    controls.target.set(state.target[0], state.target[1], state.target[2]);
    controls.update();
  };

  const zoomBy = (factor: number) => {
    const offset = camera.position.clone().sub(controls.target);
    offset.multiplyScalar(1 / factor);
    const len = THREE.MathUtils.clamp(offset.length(), planetRadius * 0.5, planetRadius * 4.2);
    offset.setLength(len);
    camera.position.copy(controls.target).add(offset);
    controls.update();
  };

  if (restoreCamera) {
    applyCameraState(restoreCamera);
  } else {
    fitToView();
  }

  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();

  const pickTile = (clientX: number, clientY: number): TileRecord | null => {
    const rect = renderer.domElement.getBoundingClientRect();
    if (rect.width <= 0 || rect.height <= 0) return null;
    pointer.x = ((clientX - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((clientY - rect.top) / rect.height) * 2 + 1;
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(pickables, false);
    if (hits.length === 0) return null;
    const key = hits[0].object.userData.key as string;
    return tileRecords.get(key) ?? null;
  };

  const onPointerMove = (e: PointerEvent) => {
    const record = pickTile(e.clientX, e.clientY);
    const key = record ? tileKey(record.tile) : null;
    if (key === hoveredKey) return;
    applyHover(key);
    callbacks.onHoverChange(record?.tile ?? null);
    renderer.domElement.style.cursor = record && getClaimable(record.tile) ? 'pointer' : '';
  };

  const onClick = (e: PointerEvent) => {
    if (e.shiftKey) return;
    const record = pickTile(e.clientX, e.clientY);
    if (!record) return;
    const { tile } = record;
    setSelectedKey(tileKey(tile));
    callbacks.onTileSelect(tile);
    if (getClaimable(tile)) callbacks.onTileClaim(tile);
  };

  const onContextMenu = (e: Event) => e.preventDefault();

  renderer.domElement.addEventListener('pointermove', onPointerMove);
  renderer.domElement.addEventListener('click', onClick);
  renderer.domElement.addEventListener('contextmenu', onContextMenu);

  let frameId = 0;
  const animate = () => {
    frameId = requestAnimationFrame(animate);
    controls.update();
    renderer.render(scene, camera);
  };
  animate();

  const onResize = () => {
    const w = host.clientWidth;
    const h = host.clientHeight;
    if (w <= 0 || h <= 0) return;
    camera.aspect = w / h;
    camera.updateProjectionMatrix();
    renderer.setSize(w, h, false);
  };

  const resizeObserver = new ResizeObserver(onResize);
  resizeObserver.observe(host);
  onResize();

  const syncMap = (
    nextMap: HexMap,
    nextCivId: string | null,
    nextDisabled: boolean,
    nextCanInteract: boolean,
  ) => {
    liveMap = nextMap;
    liveCivId = nextCivId;
    liveDisabled = nextDisabled;
    liveCanInteract = nextCanInteract;

    for (const tile of nextMap.tiles) {
      if (tile.biome === 'Ocean') continue;
      const key = tileKey(tile);
      const record = tileRecords.get(key);
      if (!record) continue;

      const prevOwner = record.tile.controllingCivilizationId;
      const prevCapital = record.tile.isCapital;
      record.tile = tile;

      if (prevOwner !== tile.controllingCivilizationId) {
        removeMarker(record.overlay);
        record.overlay = buildOwnerOverlay(tile, record.surface, record.normal, record.extrude);
      }

      if (prevCapital !== tile.isCapital) {
        removeMarker(record.capital);
        record.capital = buildCapital(tile, record.surface, record.normal, record.extrude);
      }
    }
  };

  return {
    setSelected(tile) {
      setSelectedKey(tile ? tileKey(tile) : null);
    },
    syncMap,
    zoomBy,
    fitToView,
    getCameraState(): CameraState {
      return {
        position: camera.position.toArray() as [number, number, number],
        target: controls.target.toArray() as [number, number, number],
      };
    },
    destroy() {
      cancelAnimationFrame(frameId);
      resizeObserver.disconnect();
      renderer.domElement.removeEventListener('pointermove', onPointerMove);
      renderer.domElement.removeEventListener('click', onClick);
      renderer.domElement.removeEventListener('contextmenu', onContextMenu);
      controls.dispose();
      disposables.forEach((g) => g.dispose());
      materials.forEach((m) => m.dispose());
      renderer.dispose();
      if (renderer.domElement.parentElement === host) {
        host.removeChild(renderer.domElement);
      }
    },
  };
}
