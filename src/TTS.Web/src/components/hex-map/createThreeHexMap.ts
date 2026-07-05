import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import type { HexMap, HexTile } from '../../api';
import {
  axialToPixel,
  BIOME_COLORS,
  CIV_FILL_COLORS,
  canClaim,
  columnHeight,
  computeMapLayout,
  parseCssColor,
  tileKey,
} from './hexMapModel';
import {
  computeGlobeLayout,
  flatToGlobe,
  flatToGlobeFrame,
  isInsideWorldDisc,
  orientHexOnGlobe,
  placeExtrudedOnGlobe,
  surfacePoint,
  type GlobeLayout,
} from './globeProjection';

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
  tangentX: THREE.Vector3;
  extrude: number;
  radius: number;
  overlay: THREE.Mesh | null;
  capital: THREE.Mesh | null;
};

const INITIAL_VIEW_ZOOM = 2.1;
const HEX_ROTATION = Math.PI / 6;
const HEX_SEAM = 1.16;
const NEIGHBOR_OFFSETS: [number, number][] = [
  [1, 0],
  [1, -1],
  [0, -1],
  [-1, 0],
  [-1, 1],
  [0, 1],
];
const SELECT_EMISSIVE = 0x4cd7f6;
const HOVER_EMISSIVE = 0x2a8a9e;
const GLOBE_CENTER = new THREE.Vector3(0, 0, 0);
const LIGHTEN = new THREE.Color(0xffffff);

function toThreeColor(css: string): THREE.Color {
  return new THREE.Color(parseCssColor(css).color);
}

function lightenBiomeColor(biome: string): THREE.Color {
  const color = toThreeColor(BIOME_COLORS[biome] ?? '#64748b');
  color.lerp(LIGHTEN, 0.42);
  return color;
}

function createHexColumnGeometry(radius: number, height: number): THREE.CylinderGeometry {
  const geo = new THREE.CylinderGeometry(radius, radius, height, 6, 1);
  geo.rotateY(HEX_ROTATION);
  return geo;
}

function buildGlobePlacements(
  tiles: HexTile[],
  hexSize: number,
  globe: GlobeLayout,
): Map<string, ReturnType<typeof flatToGlobeFrame>> {
  const placements = new Map<string, ReturnType<typeof flatToGlobeFrame>>();
  for (const tile of tiles) {
    const p = axialToPixel(tile.q, tile.r, hexSize);
    placements.set(tileKey(tile.q, tile.r), flatToGlobeFrame(p.x, p.y, globe));
  }
  return placements;
}

function neighborSpanOnGlobe(
  a: ReturnType<typeof flatToGlobeFrame>,
  b: ReturnType<typeof flatToGlobeFrame>,
  globe: GlobeLayout,
): number {
  const arc = a.normal.angleTo(b.normal) * globe.radius;
  const chord = a.position.distanceTo(b.position);
  return Math.max(arc, chord);
}

function hexRadiusForTile(
  tile: HexTile,
  placements: Map<string, ReturnType<typeof flatToGlobeFrame>>,
  globe: GlobeLayout,
  fallback: number,
): number {
  const self = placements.get(tileKey(tile.q, tile.r));
  if (!self) return fallback;

  let minSpan = Infinity;
  for (const [dq, dr] of NEIGHBOR_OFFSETS) {
    const neighbor = placements.get(tileKey(tile.q + dq, tile.r + dr));
    if (!neighbor) continue;
    minSpan = Math.min(minSpan, neighborSpanOnGlobe(self, neighbor, globe));
  }

  if (!Number.isFinite(minSpan)) return fallback;
  return (minSpan / Math.sqrt(3)) * HEX_SEAM;
}

function computeFallbackHexRadius(
  hexSize: number,
  globe: GlobeLayout,
  tiles: HexTile[],
  placements: Map<string, ReturnType<typeof flatToGlobeFrame>>,
): number {
  let minSpan = Infinity;
  for (const tile of tiles) {
    if (tile.biome === 'Ocean') continue;
    const self = placements.get(tileKey(tile.q, tile.r));
    if (!self) continue;
    for (const [dq, dr] of NEIGHBOR_OFFSETS) {
      const neighbor = placements.get(tileKey(tile.q + dq, tile.r + dr));
      if (!neighbor) continue;
      minSpan = Math.min(minSpan, neighborSpanOnGlobe(self, neighbor, globe));
    }
  }

  if (!Number.isFinite(minSpan)) {
    const sample = tiles.find((t) => t.biome !== 'Ocean') ?? tiles[0];
    if (!sample) return hexSize * 0.4;
    const p0 = axialToPixel(sample.q, sample.r, hexSize);
    const p1 = axialToPixel(sample.q + 1, sample.r, hexSize);
    const f0 = flatToGlobeFrame(p0.x, p0.y, globe);
    const f1 = flatToGlobeFrame(p1.x, p1.y, globe);
    minSpan = neighborSpanOnGlobe(f0, f1, globe);
  }

  return (minSpan / Math.sqrt(3)) * HEX_SEAM;
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

function focusOnGlobe(
  tiles: HexTile[],
  hexSize: number,
  globe: GlobeLayout,
  myCivilizationId: string | null,
): THREE.Vector3 {
  const owned = tiles.filter(
    (t) => myCivilizationId && t.controllingCivilizationId === myCivilizationId,
  );
  const sample = owned.length > 0 ? owned : tiles.filter((t) => t.biome !== 'Ocean').slice(0, 40);
  if (sample.length === 0) {
    return new THREE.Vector3(globe.radius, 0, 0);
  }

  const sum = new THREE.Vector3();
  for (const tile of sample) {
    const p = axialToPixel(tile.q, tile.r, hexSize);
    sum.add(flatToGlobe(p.x, p.y, globe).position);
  }
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

  const layout = computeMapLayout(map.tiles, hexSize);
  const globe = computeGlobeLayout(layout);
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0a1e32);
  scene.fog = new THREE.Fog(0x0a1e32, globe.radius * 2, globe.radius * 5.5);

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
  sun.position.set(globe.radius * 1.8, globe.radius * 1.2, -globe.radius);
  scene.add(sun);
  const fill = new THREE.DirectionalLight(0x9ee8ff, 0.52);
  fill.position.set(-globe.radius * 1.4, globe.radius * 0.4, globe.radius);
  scene.add(fill);
  const rim = new THREE.DirectionalLight(0xd4e4fa, 0.35);
  rim.position.set(0, -globe.radius, globe.radius * 1.2);
  scene.add(rim);

  const terrain = new THREE.Group();
  const markers = new THREE.Group();
  const highlights = new THREE.Group();
  scene.add(terrain);
  scene.add(markers);
  scene.add(highlights);

  const disposables: THREE.BufferGeometry[] = [];
  const materials: THREE.Material[] = [];

  const oceanShell = new THREE.Mesh(
    new THREE.SphereGeometry(globe.radius * 0.992, 72, 48),
    new THREE.MeshStandardMaterial({ color: 0x1a5f7a, roughness: 0.75, metalness: 0.05 }),
  );
  terrain.add(oceanShell);

  const atmosphere = new THREE.Mesh(
    new THREE.SphereGeometry(globe.radius * 1.045, 48, 32),
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

  const globePlacements = buildGlobePlacements(map.tiles, hexSize, globe);
  const fallbackHexRadius = computeFallbackHexRadius(hexSize, globe, map.tiles, globePlacements);

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
    tangentX: THREE.Vector3,
    extrude: number,
    radius: number,
  ): THREE.Mesh | null => {
    const ownerFill = tile.controllingCivilizationId
      ? CIV_FILL_COLORS[tile.controllingCivilizationId]
      : null;
    if (!ownerFill) return null;

    const capH = hexSize * 0.06;
    const overlayGeo = createHexColumnGeometry(radius * 0.76, capH);
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
    placeExtrudedOnGlobe(overlay, surface, normal, extrude + capH * 0.5, 0.5, tangentX);
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

  for (const tile of map.tiles) {
    if (tile.biome === 'Ocean') continue;

    const p = axialToPixel(tile.q, tile.r, hexSize);
    if (!isInsideWorldDisc(p.x, p.y, globe)) continue;

    const frame = globePlacements.get(tileKey(tile.q, tile.r)) ?? flatToGlobeFrame(p.x, p.y, globe);
    const extrude = columnHeight(tile.biome, hexSize);
    const radius = hexRadiusForTile(tile, globePlacements, globe, fallbackHexRadius);
    const geometry = createHexColumnGeometry(radius, extrude);
    disposables.push(geometry);

    const material = new THREE.MeshStandardMaterial({
      color: lightenBiomeColor(tile.biome),
      roughness: 0.62,
      metalness: 0.02,
      flatShading: true,
    });
    materials.push(material);

    const mesh = new THREE.Mesh(geometry, material);
    placeExtrudedOnGlobe(mesh, frame.position, frame.normal, extrude, 0.5, frame.tangentX);
    mesh.userData = { tile, key: tileKey(tile.q, tile.r) };
    terrain.add(mesh);
    pickables.push(mesh);

    const overlay = buildOwnerOverlay(
      tile,
      frame.position,
      frame.normal,
      frame.tangentX,
      extrude,
      radius,
    );
    const capital = buildCapital(tile, frame.position, frame.normal, extrude);

    tileRecords.set(tileKey(tile.q, tile.r), {
      tile,
      mesh,
      material,
      surface: frame.position,
      normal: frame.normal,
      tangentX: frame.tangentX,
      extrude,
      radius,
      overlay,
      capital,
    });
  }

  const selectionRing = new THREE.Mesh(
    createHexColumnGeometry(fallbackHexRadius * 1.02, hexSize * 0.04),
    new THREE.MeshStandardMaterial({
      color: 0xd4e4fa,
      emissive: 0x4cd7f6,
      emissiveIntensity: 0.65,
      transparent: true,
      opacity: 0.9,
      flatShading: true,
    }),
  );
  selectionRing.visible = false;
  highlights.add(selectionRing);
  materials.push(selectionRing.material as THREE.Material);
  disposables.push(selectionRing.geometry as THREE.BufferGeometry);

  const viewFocus = focusOnGlobe(map.tiles, hexSize, globe, myCivilizationId);

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.06;
  controls.minDistance = globe.radius * 0.5;
  controls.maxDistance = globe.radius * 4.2;
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
    orientHexOnGlobe(selectionRing, record.normal, record.tangentX);
    const ringScale = record.radius / (fallbackHexRadius * 1.02);
    selectionRing.scale.set(ringScale, 1, ringScale);
    selectionRing.position.copy(
      surfacePoint(record.surface, record.normal, record.extrude + hexSize * 0.05),
    );
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
    const distance = (globe.radius * 2.35) / zoomMultiplier;
    const lookDir = viewFocus.lengthSq() > 1e-6
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
    const len = THREE.MathUtils.clamp(offset.length(), globe.radius * 0.5, globe.radius * 4.2);
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
    const key = record ? tileKey(record.tile.q, record.tile.r) : null;
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
    setSelectedKey(tileKey(tile.q, tile.r));
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
      const key = tileKey(tile.q, tile.r);
      const record = tileRecords.get(key);
      if (!record) continue;

      const prevOwner = record.tile.controllingCivilizationId;
      const prevCapital = record.tile.isCapital;
      record.tile = tile;

      if (prevOwner !== tile.controllingCivilizationId) {
        removeMarker(record.overlay);
        record.overlay = buildOwnerOverlay(
          tile,
          record.surface,
          record.normal,
          record.tangentX,
          record.extrude,
          record.radius,
        );
      }

      if (prevCapital !== tile.isCapital) {
        removeMarker(record.capital);
        record.capital = buildCapital(tile, record.surface, record.normal, record.extrude);
      }
    }
  };

  return {
    setSelected(tile) {
      setSelectedKey(tile ? tileKey(tile.q, tile.r) : null);
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
