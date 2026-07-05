import * as THREE from 'three';
import type { MapLayout } from './hexMapModel';

/**
 * PROTOTYPE placement only — maps flat axial coords to sphere via equirectangular lat/lon.
 *
 * This cannot tessellate: a rectangular hex grid is not a valid spherical topology.
 * Replace with backend-provided PlanetTileMesh (Goldberg polyhedron) where each tile
 * carries center, normal, polygonVertices, and true spherical neighbours.
 *
 * @see planetTile.ts
 */

const POLE_TRIM = 0.06;
const LOCAL_UP = new THREE.Vector3(0, 1, 0);

export type GlobeLayout = {
  minX: number;
  maxX: number;
  minY: number;
  maxY: number;
  width: number;
  height: number;
  centerX: number;
  centerZ: number;
  halfSpan: number;
  radius: number;
  span: number;
};

export type GlobePoint = {
  position: THREE.Vector3;
  normal: THREE.Vector3;
};

export function computeGlobeLayout(layout: MapLayout): GlobeLayout {
  const centerX = (layout.minX + layout.maxX) / 2;
  const centerZ = (layout.minY + layout.maxY) / 2;
  const span = Math.max(layout.width, layout.height);
  return {
    minX: layout.minX,
    maxX: layout.maxX,
    minY: layout.minY,
    maxY: layout.maxY,
    width: layout.width,
    height: layout.height,
    centerX,
    centerZ,
    halfSpan: span / 2,
    radius: span * 0.36,
    span,
  };
}

/** @deprecated Remove when backend serves PlanetTileMesh centers directly. */
export function flatToGlobe(flatX: number, flatZ: number, globe: GlobeLayout): GlobePoint {
  const u = (flatX - globe.minX) / Math.max(globe.width, 1e-6);
  const v = (flatZ - globe.minY) / Math.max(globe.height, 1e-6);

  const lon = u * Math.PI * 2 - Math.PI;
  const latMax = Math.PI / 2 - POLE_TRIM;
  const lat = latMax - v * (2 * latMax);

  const cosLat = Math.cos(lat);
  const sinLat = Math.sin(lat);
  const sinLon = Math.sin(lon);
  const cosLon = Math.cos(lon);

  const normal = new THREE.Vector3(cosLat * sinLon, sinLat, cosLat * cosLon);
  const position = normal.clone().multiplyScalar(globe.radius);

  return { position, normal };
}

/** Rigid hex prism: align local Y to normal, base on sphere, extrude outward. Geometry is never warped. */
export function attachHexPrism(
  object: THREE.Object3D,
  center: THREE.Vector3,
  normal: THREE.Vector3,
  height: number,
) {
  const n = normal.clone().normalize();
  object.quaternion.setFromUnitVectors(LOCAL_UP, n);
  object.position.copy(center).addScaledVector(n, height * 0.5);
}

export function attachPolygonPrism(
  object: THREE.Object3D,
  center: THREE.Vector3,
  normal: THREE.Vector3,
  height: number,
) {
  attachHexPrism(object, center, normal, height);
}

export function surfacePoint(surface: THREE.Vector3, normal: THREE.Vector3, lift: number): THREE.Vector3 {
  return surface.clone().addScaledVector(normal, lift);
}

export function orientToNormal(object: THREE.Object3D, normal: THREE.Vector3) {
  object.quaternion.setFromUnitVectors(LOCAL_UP, normal.clone().normalize());
}
