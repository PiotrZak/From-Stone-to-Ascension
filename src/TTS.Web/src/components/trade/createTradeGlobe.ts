import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import type { HexTile, TradeGlobe, TradeHub, Vec3 } from '../../api';
import { tileExtrusionHeight, tileKey } from '../hex-map/hexMapModel';
import { surfacePoint } from '../hex-map/globeProjection';

export type TradeGlobeCallbacks = {
  onHover: (info: string | null) => void;
  onHubSelect: (hub: TradeHub | null) => void;
  onTileSelect: (tile: HexTile | null, tradeCountry?: string) => void;
};

export type TradeGlobeHandle = {
  applyData: (data: TradeGlobe) => void;
  setSelection: (tileId: string | null, hubId: string | null) => void;
  zoomBy: (factor: number) => void;
  fitToView: () => void;
  destroy: () => void;
};

const COUNTRY_COLORS: Record<string, string> = {
  china: '#ef4444',
  india: '#ec4899',
  'middle-east': '#a78bfa',
  nigeria: '#22c55e',
  kenya: '#3b82f6',
  tanzania: '#a855f7',
  ethiopia: '#f59e0b',
  egypt: '#eab308',
  ghana: '#14b8a6',
  'south-africa': '#f97316',
};

const CORRIDOR_COUNTRIES = new Set(['india', 'middle-east']);
const AFRICA_COUNTRIES = new Set([
  'nigeria',
  'kenya',
  'tanzania',
  'ethiopia',
  'egypt',
  'ghana',
  'south-africa',
]);

const OCEAN_COLOR = new THREE.Color(0x0c4a6e);
const LAND_FALLBACK = new THREE.Color(0x334155);
const INACTIVE_LAND = new THREE.Color(0x1e293b);
const INITIAL_VIEW_ZOOM = 0.92;
const DEFAULT_LOOK_DIR = new THREE.Vector3(0.25, 0.35, 1).normalize();

type TileRecord = {
  tile: HexTile;
  mesh: THREE.Mesh;
  material: THREE.MeshStandardMaterial;
  tradeCountry?: string;
};

type HubRecord = {
  hub: TradeHub;
  mesh: THREE.Mesh;
  material: THREE.MeshStandardMaterial;
};

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
  const center = sphere.center;
  controls.target.copy(center);

  const aspect = camera.aspect > 0 ? camera.aspect : 1;
  const fovV = (camera.fov * Math.PI) / 180;
  const fovH = 2 * Math.atan(Math.tan(fovV / 2) * aspect);
  const distV = sphere.radius / Math.sin(fovV / 2);
  const distH = sphere.radius / Math.sin(fovH / 2);
  const distance = Math.max(distV, distH) / zoom;

  camera.position.copy(center).add(DEFAULT_LOOK_DIR.clone().multiplyScalar(distance));
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

function greatCirclePoints(from: THREE.Vector3, to: THREE.Vector3, segments = 48): THREE.Vector3[] {
  const start = from.clone().normalize();
  const end = to.clone().normalize();
  const points: THREE.Vector3[] = [];
  const angle = start.angleTo(end);
  if (angle < 1e-5) return [from.clone(), to.clone()];
  for (let i = 0; i <= segments; i++) {
    const t = i / segments;
    const sinTotal = Math.sin(angle);
    const a = Math.sin((1 - t) * angle) / sinTotal;
    const b = Math.sin(t * angle) / sinTotal;
    points.push(
      start
        .clone()
        .multiplyScalar(a)
        .add(end.clone().multiplyScalar(b))
        .normalize()
        .multiplyScalar(from.length() * (1 + t * 0.08)),
    );
  }
  return points;
}

function hasActiveFilters(data: TradeGlobe): boolean {
  return Boolean(
    data.appliedCommodity || data.appliedImportCountry || data.appliedTransportMode,
  );
}

function tileColor(
  tradeCountry: string | undefined,
  intensity: number,
  active: boolean,
  filtered: boolean,
  isOcean: boolean,
): THREE.Color {
  if (isOcean) return OCEAN_COLOR.clone();
  if (!tradeCountry) return LAND_FALLBACK.clone();

  const base = new THREE.Color(COUNTRY_COLORS[tradeCountry] ?? '#64748b');
  if (filtered && !active) {
    return INACTIVE_LAND.clone().lerp(base, 0.12);
  }
  if (CORRIDOR_COUNTRIES.has(tradeCountry)) {
    base.lerp(new THREE.Color(0xffffff), filtered && active ? 0.04 : 0.08);
    return base;
  }
  base.lerp(new THREE.Color(0xffffff), 0.15 + (1 - intensity) * 0.35);
  return base;
}

function corridorWaypoint(
  from: TradeHub,
  to: TradeHub,
  hubById: Record<string, TradeHub>,
): TradeHub | null {
  if (from.countryId !== 'china' || !AFRICA_COUNTRIES.has(to.countryId)) return null;
  const westAfrica = to.countryId === 'nigeria' || to.countryId === 'ghana' || to.countryId === 'egypt';
  const waypointId = westAfrica ? 'dubai' : 'mumbai';
  return hubById[waypointId] ?? hubById.colombo ?? null;
}

function flowArcPoints(
  from: TradeHub,
  to: TradeHub,
  hubById: Record<string, TradeHub>,
  planetRadius: number,
  lift: number,
): THREE.Vector3[] {
  const fromPos = new THREE.Vector3(from.centerX, from.centerY, from.centerZ)
    .normalize()
    .multiplyScalar(planetRadius + lift);
  const toPos = new THREE.Vector3(to.centerX, to.centerY, to.centerZ)
    .normalize()
    .multiplyScalar(planetRadius + lift);
  const waypoint = corridorWaypoint(from, to, hubById);
  if (!waypoint) return greatCirclePoints(fromPos, toPos);

  const viaPos = new THREE.Vector3(waypoint.centerX, waypoint.centerY, waypoint.centerZ)
    .normalize()
    .multiplyScalar(planetRadius + lift);
  return [
    ...greatCirclePoints(fromPos, viaPos),
    ...greatCirclePoints(viaPos, toPos).slice(1),
  ];
}

export async function createTradeGlobe(
  host: HTMLElement,
  data: TradeGlobe,
  hexSize: number,
  callbacks: TradeGlobeCallbacks,
): Promise<TradeGlobeHandle> {
  await waitForHostSize(host);

  const map = data.map;
  const planetRadius = map.planetRadius > 0 ? map.planetRadius : 100;
  const tileHeight = tileExtrusionHeight(hexSize);

  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0a1e32);
  scene.fog = new THREE.Fog(0x0a1e32, planetRadius * 2, planetRadius * 5.5);

  const camera = new THREE.PerspectiveCamera(48, 1, 0.1, 5000);
  const renderer = new THREE.WebGLRenderer({ antialias: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setSize(host.clientWidth, host.clientHeight, false);
  renderer.domElement.classList.add('trade-globe-canvas');
  host.appendChild(renderer.domElement);

  scene.add(new THREE.AmbientLight(0xc8d8f0, 0.78));
  const sun = new THREE.DirectionalLight(0xfff8ee, 1.45);
  sun.position.set(planetRadius * 1.8, planetRadius * 1.2, -planetRadius);
  scene.add(sun);

  const globeRoot = new THREE.Group();
  const terrain = new THREE.Group();
  const flowGroup = new THREE.Group();
  const hubGroup = new THREE.Group();
  globeRoot.add(terrain);
  globeRoot.add(flowGroup);
  globeRoot.add(hubGroup);
  scene.add(globeRoot);

  const oceanShell = new THREE.Mesh(
    new THREE.SphereGeometry(planetRadius * 0.985, 72, 48),
    new THREE.MeshStandardMaterial({ color: OCEAN_COLOR, roughness: 0.88 }),
  );
  terrain.add(oceanShell);

  const tileRecords = new Map<string, TileRecord>();
  const sortedTiles = [...map.tiles].sort((a, b) => {
    if (a.biome === 'Ocean' && b.biome !== 'Ocean') return -1;
    if (a.biome !== 'Ocean' && b.biome === 'Ocean') return 1;
    return 0;
  });

  for (const tile of sortedTiles) {
    if (!tile.polygonVertices?.length) continue;
    const normal = tileNormal(tile);
    const geo = createWorldSpacePrism(tile.polygonVertices, normal, tileHeight);
    const tradeCountry = data.tradeCountryByTileId[tile.id];
    const isOcean = tile.biome === 'Ocean';
    const material = new THREE.MeshStandardMaterial({
      color: OCEAN_COLOR,
      roughness: isOcean ? 0.78 : 0.62,
      flatShading: true,
      transparent: !isOcean,
      opacity: 1,
    });
    const mesh = new THREE.Mesh(geo, material);
    mesh.userData = { tile, tradeCountry, key: tileKey(tile) };
    terrain.add(mesh);
    if (!isOcean) {
      tileRecords.set(tileKey(tile), { tile, mesh, material, tradeCountry });
    }
  }

  const hubRecords: HubRecord[] = data.hubs.map((hub) => {
    const pos = new THREE.Vector3(hub.centerX, hub.centerY, hub.centerZ);
    const normal = pos.clone().normalize();
    const material = new THREE.MeshStandardMaterial({
      color: COUNTRY_COLORS[hub.countryId] ?? '#ffffff',
      emissive: 0x222222,
      emissiveIntensity: 0.4,
      transparent: true,
      opacity: 1,
    });
    const marker = new THREE.Mesh(
      new THREE.SphereGeometry(hexSize * 0.12, 14, 12),
      material,
    );
    marker.position.copy(surfacePoint(pos, normal, tileHeight + hexSize * 0.1));
    marker.userData = { hub };
    hubGroup.add(marker);
    return { hub, mesh: marker, material };
  });

  let liveData = data;
  let statsByCountry = Object.fromEntries(data.countries.map((c) => [c.countryId, c]));
  let activeCountries = new Set(data.activeCountryIds);
  let activeHubs = new Set(data.activeHubIds);
  let hubById = Object.fromEntries(data.hubs.map((h) => [h.id, h]));
  let selectedTileId: string | null = null;
  let selectedHubId: string | null = null;
  const flowLines: THREE.Line[] = [];

  const syncTiles = () => {
    const filtered = hasActiveFilters(liveData);
    for (const record of tileRecords.values()) {
      const { tradeCountry, material, tile } = record;
      const isOcean = tile.biome === 'Ocean';
      const stats = tradeCountry ? statsByCountry[tradeCountry] : undefined;
      const active = tradeCountry ? activeCountries.has(tradeCountry) : false;
      const selected = selectedTileId === tile.id;
      const intensity =
        stats && liveData.maxCountryValueUsd > 0
          ? stats.totalValueUsd / liveData.maxCountryValueUsd
          : 0;
      material.color.copy(tileColor(tradeCountry, intensity, active, filtered, isOcean));
      material.opacity = filtered && tradeCountry && !active ? 0.45 : 1;
      if (selected) {
        material.emissive.setHex(0x4cd7f6);
        material.emissiveIntensity = 0.85;
      } else {
        material.emissive.setHex(filtered && active && stats && stats.totalValueUsd > 0 ? 0x0a1520 : 0x000000);
        material.emissiveIntensity = filtered && active && stats && stats.totalValueUsd > 0 ? 0.35 : 0;
      }
    }
  };

  const syncFlowHighlight = () => {
    const maxFlow = Math.max(...liveData.flows.map((f) => f.totalValueUsd), 1);
    for (const line of flowLines) {
      const flow = line.userData.flow as TradeGlobe['flows'][number];
      const connected =
        !selectedHubId || flow.fromHubId === selectedHubId || flow.toHubId === selectedHubId;
      const base = 0.3 + (flow.totalValueUsd / maxFlow) * 0.6;
      const material = line.material as THREE.LineBasicMaterial;
      material.opacity = connected ? base : Math.max(0.04, base * 0.1);
    }
  };

  const rebuildFlows = () => {
    while (flowGroup.children.length > 0) {
      const child = flowGroup.children[0];
      flowGroup.remove(child);
      if (child instanceof THREE.Line) {
        child.geometry.dispose();
        (child.material as THREE.Material).dispose();
      }
    }
    flowLines.length = 0;

    const maxFlow = Math.max(...liveData.flows.map((f) => f.totalValueUsd), 1);
    for (const flow of liveData.flows) {
      const from = hubById[flow.fromHubId];
      const to = hubById[flow.toHubId];
      if (!from || !to) continue;
      const lift = tileHeight + planetRadius * 0.02;
      const points = flowArcPoints(from, to, hubById, planetRadius, lift);
      const geo = new THREE.BufferGeometry().setFromPoints(points);
      const opacity = 0.3 + (flow.totalValueUsd / maxFlow) * 0.6;
      const line = new THREE.Line(
        geo,
        new THREE.LineBasicMaterial({
          color: COUNTRY_COLORS[flow.importCountry] ?? 0x4cd7f6,
          transparent: true,
          opacity,
          linewidth: 1,
        }),
      );
      line.userData = { flow };
      flowGroup.add(line);
      flowLines.push(line);
    }
    syncFlowHighlight();
  };

  const syncHubs = () => {
    const filtered = hasActiveFilters(liveData);
    for (const { hub, material, mesh } of hubRecords) {
      const active = activeHubs.has(hub.id);
      const selected = selectedHubId === hub.id;
      const base = new THREE.Color(COUNTRY_COLORS[hub.countryId] ?? '#ffffff');
      material.color.copy(filtered && !active ? INACTIVE_LAND.clone().lerp(base, 0.25) : base);
      material.opacity = filtered && !active ? 0.25 : 1;
      material.emissive.setHex(selected ? 0x4cd7f6 : 0x222222);
      material.emissiveIntensity = selected ? 0.9 : filtered && active ? 0.65 : 0.35;
      mesh.scale.setScalar(selected ? 1.45 : 1);
    }
  };

  const setSelection = (tileId: string | null, hubId: string | null) => {
    selectedTileId = tileId;
    selectedHubId = hubId;
    syncTiles();
    syncHubs();
    syncFlowHighlight();
  };

  const applyData = (next: TradeGlobe) => {
    liveData = next;
    statsByCountry = Object.fromEntries(next.countries.map((c) => [c.countryId, c]));
    activeCountries = new Set(next.activeCountryIds);
    activeHubs = new Set(next.activeHubIds);
    hubById = Object.fromEntries(next.hubs.map((h) => [h.id, h]));
    syncTiles();
    syncHubs();
    rebuildFlows();
  };

  applyData(data);

  const controls = new OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.06;
  controls.screenSpacePanning = false;
  controls.minDistance = planetRadius * 0.5;
  controls.maxDistance = planetRadius * 4.2;

  const fitToView = () => {
    frameGlobe(camera, controls, globeRoot);
  };

  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();
  const pickables = () =>
    [...tileRecords.values()].map((r) => r.mesh).concat(hubRecords.map((r) => r.mesh));

  const onPointerMove = (e: PointerEvent) => {
    const rect = renderer.domElement.getBoundingClientRect();
    pointer.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(pickables(), false);
    if (hits.length === 0) {
      callbacks.onHover(null);
      renderer.domElement.style.cursor = '';
      return;
    }
    const obj = hits[0].object;
    const canPick =
      obj.userData.hub
        ? !hasActiveFilters(liveData) || activeHubs.has((obj.userData.hub as TradeHub).id)
        : obj.userData.tile &&
          (obj.userData.tile as HexTile).biome !== 'Ocean' &&
          (!hasActiveFilters(liveData) ||
            !obj.userData.tradeCountry ||
            activeCountries.has(obj.userData.tradeCountry as string));
    renderer.domElement.style.cursor = canPick ? 'pointer' : '';
    if (obj.userData.hub) {
      const hub = obj.userData.hub as TradeHub;
      const stats = statsByCountry[hub.countryId];
      const inactive = hasActiveFilters(liveData) && !activeHubs.has(hub.id);
      callbacks.onHover(
        inactive
          ? `${hub.name} · not in current filter`
          : `${hub.name} · ${stats?.displayName ?? hub.countryId} · ${stats ? `$${Math.round(stats.totalValueUsd).toLocaleString()}` : 'Corridor'}`,
      );
      return;
    }
    const tradeCountry = obj.userData.tradeCountry as string | undefined;
    const stats = tradeCountry ? statsByCountry[tradeCountry] : undefined;
    const inactive = tradeCountry && hasActiveFilters(liveData) && !activeCountries.has(tradeCountry);
    callbacks.onHover(
      inactive
        ? `${stats?.displayName ?? tradeCountry ?? 'Land'} · filtered out`
        : stats
          ? `${stats.displayName} · $${Math.round(stats.totalValueUsd).toLocaleString()} · ${stats.shipmentCount} shipments`
          : 'Ocean',
    );
  };

  const onClick = (e: PointerEvent) => {
    const rect = renderer.domElement.getBoundingClientRect();
    pointer.x = ((e.clientX - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((e.clientY - rect.top) / rect.height) * 2 + 1;
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(pickables(), false);
    if (hits.length === 0) {
      callbacks.onHubSelect(null);
      callbacks.onTileSelect(null);
      setSelection(null, null);
      return;
    }

    const obj = hits[0].object;
    if (obj.userData.hub) {
      const hub = obj.userData.hub as TradeHub;
      if (hasActiveFilters(liveData) && !activeHubs.has(hub.id)) {
        callbacks.onHubSelect(null);
        callbacks.onTileSelect(null);
        setSelection(null, null);
        return;
      }
      callbacks.onHubSelect(hub);
      callbacks.onTileSelect(null);
      setSelection(null, hub.id);
      return;
    }

    const tile = obj.userData.tile as HexTile | undefined;
    const tradeCountry = obj.userData.tradeCountry as string | undefined;
    if (!tile || tile.biome === 'Ocean') {
      callbacks.onHubSelect(null);
      callbacks.onTileSelect(null);
      setSelection(null, null);
      return;
    }
    if (tradeCountry && hasActiveFilters(liveData) && !activeCountries.has(tradeCountry)) {
      callbacks.onHubSelect(null);
      callbacks.onTileSelect(null);
      setSelection(null, null);
      return;
    }
    callbacks.onHubSelect(null);
    callbacks.onTileSelect(tile, tradeCountry);
    setSelection(tile.id, null);
  };

  renderer.domElement.addEventListener('pointermove', onPointerMove);
  renderer.domElement.addEventListener('click', onClick);

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
  const ro = new ResizeObserver(onResize);
  ro.observe(host);
  onResize();
  fitToView();

  return {
    applyData,
    setSelection,
    zoomBy(factor: number) {
      const offset = camera.position.clone().sub(controls.target);
      offset.multiplyScalar(1 / factor);
      camera.position.copy(controls.target).add(offset);
      controls.update();
    },
    fitToView,
    destroy() {
      cancelAnimationFrame(frameId);
      ro.disconnect();
      renderer.domElement.removeEventListener('pointermove', onPointerMove);
      renderer.domElement.removeEventListener('click', onClick);
      controls.dispose();
      renderer.dispose();
      host.removeChild(renderer.domElement);
    },
  };
}
