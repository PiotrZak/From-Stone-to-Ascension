import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import type {
  HexTile,
  SatelliteBody,
  SatelliteGlobe,
  SatelliteGroundStation,
  Vec3,
} from '../../api';
import { tileExtrusionHeight, tileKey } from '../hex-map/hexMapModel';
import { TRADE_GLOBE_PALETTES, type TradeGlobeTheme } from '../trade/tradeGlobeTheme';

export type SatelliteGlobeCallbacks = {
  onHover: (info: string | null) => void;
  onSatelliteSelect: (sat: SatelliteBody | null) => void;
  onStationSelect: (station: SatelliteGroundStation | null) => void;
};

export type SatelliteGlobeHandle = {
  applyData: (data: SatelliteGlobe) => void;
  setSelection: (satelliteId: string | null) => void;
  setTheme: (theme: TradeGlobeTheme) => void;
  destroy: () => void;
};

type TileRecord = {
  tile: HexTile;
  mesh: THREE.Mesh;
  material: THREE.MeshStandardMaterial;
};

type SatRecord = {
  body: SatelliteBody;
  mesh: THREE.Group;
  orbit: THREE.LineLoop;
};

const INITIAL_VIEW_ZOOM = 0.88;
const DEFAULT_LOOK_DIR = new THREE.Vector3(0.2, 0.45, 1).normalize();
const LAND_BASE = new THREE.Color(0x1e293b);
const OCEAN_BASE = new THREE.Color(0x0c4a6e);

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

function frameGlobe(
  camera: THREE.PerspectiveCamera,
  controls: OrbitControls,
  root: THREE.Object3D,
  zoom = INITIAL_VIEW_ZOOM,
) {
  const sphere = new THREE.Box3().setFromObject(root).getBoundingSphere(new THREE.Sphere());
  controls.target.copy(sphere.center);
  const aspect = camera.aspect > 0 ? camera.aspect : 1;
  const fovV = (camera.fov * Math.PI) / 180;
  const fovH = 2 * Math.atan(Math.tan(fovV / 2) * aspect);
  const distV = sphere.radius / Math.sin(fovV / 2);
  const distH = sphere.radius / Math.sin(fovH / 2);
  const distance = Math.max(distV, distH) / zoom;
  camera.position.copy(sphere.center).add(DEFAULT_LOOK_DIR.clone().multiplyScalar(distance));
  camera.near = Math.max(0.1, distance / 200);
  camera.far = Math.max(camera.near + 1, distance * 20);
  camera.updateProjectionMatrix();
  controls.update();
}

function tileNormal(tile: HexTile): THREE.Vector3 {
  return new THREE.Vector3(tile.normalX, tile.normalY, tile.normalZ).normalize();
}

function createWorldSpacePrism(
  polygon: Vec3[],
  normal: THREE.Vector3,
  height: number,
): THREE.BufferGeometry {
  const n = polygon.length;
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
    indices.push(bottomIdx[i], bottomIdx[j], topIdx[j], bottomIdx[i], topIdx[j], topIdx[i]);
  }
  const topCenter = top.reduce((s, v) => s.add(v), new THREE.Vector3()).divideScalar(n);
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

function createSatelliteMesh(scale: number, color: string): THREE.Group {
  const group = new THREE.Group();
  const accent = new THREE.Color(color);
  const body = new THREE.Mesh(
    new THREE.BoxGeometry(scale * 0.55, scale * 0.35, scale * 0.35),
    new THREE.MeshStandardMaterial({
      color: 0xe2e8f0,
      metalness: 0.7,
      roughness: 0.3,
      emissive: accent,
      emissiveIntensity: 0.25,
    }),
  );
  const panelMat = new THREE.MeshStandardMaterial({
    color: accent.clone().multiplyScalar(0.35),
    metalness: 0.45,
    roughness: 0.4,
    emissive: accent,
    emissiveIntensity: 0.35,
  });
  const left = new THREE.Mesh(new THREE.BoxGeometry(scale * 1.15, scale * 0.08, scale * 0.55), panelMat);
  left.position.x = -scale * 0.95;
  const right = left.clone();
  right.position.x = scale * 0.95;
  group.add(body, left, right);
  return group;
}

function coverageColor(coverage: number, maxCoverage: number, isOcean: boolean): THREE.Color {
  if (isOcean) return OCEAN_BASE.clone().lerp(new THREE.Color(0x0369a1), 0.35);
  const t = maxCoverage > 0 ? Math.min(1, coverage / maxCoverage) : 0;
  return LAND_BASE.clone().lerp(new THREE.Color(0x38bdf8), 0.15 + t * 0.75);
}

function orbitPosition(
  planetRadius: number,
  altitudeFactor: number,
  inclination: number,
  phase: number,
  elapsed: number,
  speed: number,
): THREE.Vector3 {
  const radius = planetRadius * altitudeFactor;
  const angle = phase + elapsed * speed;
  const x = Math.cos(angle) * radius;
  const z = Math.sin(angle) * radius;
  const y = Math.sin(angle) * Math.sin(inclination) * radius * 0.85;
  return new THREE.Vector3(x, y, z);
}

export async function createSatelliteGlobe(
  host: HTMLElement,
  data: SatelliteGlobe,
  hexSize: number,
  callbacks: SatelliteGlobeCallbacks,
  initialTheme: TradeGlobeTheme = 'dark',
): Promise<SatelliteGlobeHandle> {
  await waitForHostSize(host);

  const map = data.map;
  const planetRadius = map.planetRadius > 0 ? map.planetRadius : 100;
  const tileHeight = tileExtrusionHeight(hexSize);
  let currentTheme = initialTheme;
  let palette = TRADE_GLOBE_PALETTES[currentTheme];
  let liveData = data;
  let selectedSatelliteId: string | null = null;
  let colorByConstellation = Object.fromEntries(
    data.constellations.map((c) => [c.id, c.color]),
  );

  const scene = new THREE.Scene();
  scene.background = new THREE.Color(palette.sceneBg);
  scene.fog = new THREE.Fog(palette.fog, planetRadius * 2.4, planetRadius * 6.2);

  const camera = new THREE.PerspectiveCamera(48, 1, 0.1, 5000);
  const renderer = new THREE.WebGLRenderer({ antialias: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setSize(host.clientWidth, host.clientHeight, false);
  renderer.domElement.classList.add('trade-globe-canvas');
  host.appendChild(renderer.domElement);

  const ambientLight = new THREE.AmbientLight(palette.ambient, palette.ambientIntensity * 0.85);
  scene.add(ambientLight);
  const sun = new THREE.DirectionalLight(palette.sun, palette.sunIntensity);
  sun.position.set(planetRadius * 1.8, planetRadius * 1.2, -planetRadius);
  scene.add(sun);

  const globeRoot = new THREE.Group();
  const terrain = new THREE.Group();
  const orbitGroup = new THREE.Group();
  const stationGroup = new THREE.Group();
  globeRoot.add(terrain);
  scene.add(globeRoot);
  scene.add(orbitGroup);
  scene.add(stationGroup);

  const oceanMaterial = new THREE.MeshStandardMaterial({
    color: palette.ocean,
    roughness: 0.9,
  });
  terrain.add(
    new THREE.Mesh(new THREE.SphereGeometry(planetRadius * 0.985, 72, 48), oceanMaterial),
  );

  const tileRecords = new Map<string, TileRecord>();
  for (const tile of map.tiles) {
    if (!tile.polygonVertices?.length) continue;
    const normal = tileNormal(tile);
    const geo = createWorldSpacePrism(tile.polygonVertices, normal, tileHeight);
    const isOcean = tile.biome === 'Ocean';
    const material = new THREE.MeshStandardMaterial({
      color: isOcean ? palette.ocean : LAND_BASE.getHex(),
      roughness: isOcean ? 0.82 : 0.68,
      flatShading: true,
      transparent: true,
      opacity: isOcean ? 0.95 : 1,
    });
    const mesh = new THREE.Mesh(geo, material);
    mesh.userData = { tile, key: tileKey(tile) };
    terrain.add(mesh);
    tileRecords.set(tileKey(tile), { tile, mesh, material });
  }

  let satRecords: SatRecord[] = [];
  let stationMeshes: THREE.Mesh[] = [];

  const syncTiles = () => {
    const max = liveData.maxCoverage || 1;
    for (const record of tileRecords.values()) {
      const coverage = liveData.coverageByTileId[record.tile.id] ?? 0;
      const isOcean = record.tile.biome === 'Ocean';
      record.material.color.copy(coverageColor(coverage, max, isOcean));
      record.material.emissive.setHex(isOcean ? 0x000000 : 0x0ea5e9);
      record.material.emissiveIntensity = isOcean ? 0 : (coverage / max) * 0.35;
    }
  };

  const clearOrbits = () => {
    while (orbitGroup.children.length > 0) {
      const child = orbitGroup.children[0];
      orbitGroup.remove(child);
      child.traverse((obj) => {
        if (obj instanceof THREE.Mesh || obj instanceof THREE.Line) {
          obj.geometry.dispose();
          const mat = obj.material;
          if (Array.isArray(mat)) mat.forEach((m) => m.dispose());
          else mat.dispose();
        }
      });
    }
    satRecords = [];
  };

  const clearStations = () => {
    while (stationGroup.children.length > 0) {
      const child = stationGroup.children[0];
      stationGroup.remove(child);
      if (child instanceof THREE.Mesh) {
        child.geometry.dispose();
        (child.material as THREE.Material).dispose();
      }
    }
    stationMeshes = [];
  };

  const rebuildOrbits = () => {
    clearOrbits();
    const scale = Math.max(hexSize * 0.4, planetRadius * 0.014);
    const drawnOrbits = new Set<string>();

    for (const body of liveData.satellites) {
      const color = colorByConstellation[body.constellationId] ?? '#38bdf8';
      const radius = planetRadius * body.altitudeFactor;
      const orbitKey = `${body.constellationId}:${body.altitudeFactor.toFixed(2)}:${body.inclinationRad.toFixed(2)}`;
      let orbit: THREE.LineLoop | null = null;
      if (!drawnOrbits.has(orbitKey)) {
        drawnOrbits.add(orbitKey);
        orbit = new THREE.LineLoop(
          new THREE.BufferGeometry().setFromPoints(
            Array.from({ length: 128 }, (_, i) => {
              const a = (i / 128) * Math.PI * 2;
              return new THREE.Vector3(
                Math.cos(a) * radius,
                Math.sin(a) * Math.sin(body.inclinationRad) * radius * 0.85,
                Math.sin(a) * radius,
              );
            }),
          ),
          new THREE.LineBasicMaterial({
            color,
            transparent: true,
            opacity: 0.28,
          }),
        );
        orbitGroup.add(orbit);
      }

      const mesh = createSatelliteMesh(scale, color);
      mesh.userData = { satellite: body };
      orbitGroup.add(mesh);
      satRecords.push({
        body,
        mesh,
        orbit: orbit ?? (orbitGroup.children[0] as THREE.LineLoop),
      });
    }
  };

  const rebuildStations = () => {
    clearStations();
    for (const station of liveData.groundStations) {
      const color = colorByConstellation[station.constellationId] ?? '#ffffff';
      const material = new THREE.MeshStandardMaterial({
        color,
        emissive: new THREE.Color(color),
        emissiveIntensity: 0.55,
      });
      const mesh = new THREE.Mesh(new THREE.CylinderGeometry(hexSize * 0.08, hexSize * 0.12, hexSize * 0.35, 8), material);
      const pos = new THREE.Vector3(station.centerX, station.centerY, station.centerZ);
      const normal = pos.clone().normalize();
      mesh.position.copy(pos).addScaledVector(normal, tileHeight + hexSize * 0.2);
      mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), normal);
      mesh.userData = { station };
      stationGroup.add(mesh);
      stationMeshes.push(mesh);
    }
  };

  const syncSelection = () => {
    for (const { body, mesh } of satRecords) {
      const selected = body.id === selectedSatelliteId;
      mesh.scale.setScalar(selected ? 1.55 : 1);
    }
  };

  const applyData = (next: SatelliteGlobe) => {
    liveData = next;
    colorByConstellation = Object.fromEntries(next.constellations.map((c) => [c.id, c.color]));
    syncTiles();
    rebuildOrbits();
    rebuildStations();
    syncSelection();
  };

  const setSelection = (satelliteId: string | null) => {
    selectedSatelliteId = satelliteId;
    syncSelection();
  };

  const setTheme = (next: TradeGlobeTheme) => {
    if (next === currentTheme) return;
    currentTheme = next;
    palette = TRADE_GLOBE_PALETTES[next];
    scene.background = new THREE.Color(palette.sceneBg);
    scene.fog!.color.setHex(palette.fog);
    oceanMaterial.color.setHex(palette.ocean);
    ambientLight.color.setHex(palette.ambient);
    ambientLight.intensity = palette.ambientIntensity * 0.85;
    sun.color.setHex(palette.sun);
    sun.intensity = palette.sunIntensity;
    syncTiles();
  };

  applyData(data);

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.06;
  controls.screenSpacePanning = false;
  controls.minDistance = planetRadius * 0.55;
  controls.maxDistance = planetRadius * 4.5;

  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();
  const pickables = () => [
    ...satRecords.map((r) => r.mesh),
    ...stationMeshes,
  ];

  const onPointerMove = (e: PointerEvent) => {
    const rect = renderer.domElement.getBoundingClientRect();
    pointer.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(pickables(), true);
    if (hits.length === 0) {
      callbacks.onHover(null);
      renderer.domElement.style.cursor = '';
      return;
    }
    let obj: THREE.Object3D | null = hits[0].object;
    while (obj && !obj.userData.satellite && !obj.userData.station) obj = obj.parent;
    if (!obj) return;
    renderer.domElement.style.cursor = 'pointer';
    if (obj.userData.satellite) {
      const sat = obj.userData.satellite as SatelliteBody;
      callbacks.onHover(
        `${sat.name} · ${sat.orbitClass || 'Orbit'} · ${sat.country || sat.operator || sat.purpose}`,
      );
      return;
    }
    const station = obj.userData.station as SatelliteGroundStation;
    callbacks.onHover(
      `${station.name} · ${station.region}${station.launchCount ? ` · ${station.launchCount} launches` : ''}`,
    );
  };

  const onClick = (e: PointerEvent) => {
    const rect = renderer.domElement.getBoundingClientRect();
    pointer.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(pickables(), true);
    if (hits.length === 0) {
      callbacks.onSatelliteSelect(null);
      callbacks.onStationSelect(null);
      setSelection(null);
      return;
    }
    let obj: THREE.Object3D | null = hits[0].object;
    while (obj && !obj.userData.satellite && !obj.userData.station) obj = obj.parent;
    if (!obj) return;
    if (obj.userData.satellite) {
      const sat = obj.userData.satellite as SatelliteBody;
      callbacks.onStationSelect(null);
      callbacks.onSatelliteSelect(sat);
      setSelection(sat.id);
      return;
    }
    const station = obj.userData.station as SatelliteGroundStation;
    callbacks.onSatelliteSelect(null);
    callbacks.onStationSelect(station);
    setSelection(null);
  };

  renderer.domElement.addEventListener('pointermove', onPointerMove);
  renderer.domElement.addEventListener('click', onClick);

  let frameId = 0;
  const clock = new THREE.Clock();
  const animate = () => {
    frameId = requestAnimationFrame(animate);
    const elapsed = clock.getElapsedTime();
    for (const { body, mesh } of satRecords) {
      const pos = orbitPosition(
        planetRadius,
        body.altitudeFactor,
        body.inclinationRad,
        body.phaseRad,
        elapsed,
        body.angularSpeed,
      );
      mesh.position.copy(pos);
      mesh.lookAt(0, 0, 0);
      mesh.rotateY(Math.PI / 2);
    }
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
  const ro = new ResizeObserver(onResize);
  ro.observe(host);
  onResize();
  frameGlobe(camera, controls, globeRoot);

  return {
    applyData,
    setSelection,
    setTheme,
    destroy() {
      cancelAnimationFrame(frameId);
      ro.disconnect();
      renderer.domElement.removeEventListener('pointermove', onPointerMove);
      renderer.domElement.removeEventListener('click', onClick);
      clearOrbits();
      clearStations();
      controls.dispose();
      renderer.dispose();
      host.removeChild(renderer.domElement);
    },
  };
}
