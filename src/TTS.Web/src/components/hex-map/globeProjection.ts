import * as THREE from 'three';
import type { MapLayout } from './hexMapModel';

export type GlobeLayout = {
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

export type GlobeFrame = GlobePoint & {
  tangentX: THREE.Vector3;
};

export function computeGlobeLayout(layout: MapLayout): GlobeLayout {
  const centerX = (layout.minX + layout.maxX) / 2;
  const centerZ = (layout.minY + layout.maxY) / 2;
  const span = Math.max(layout.width, layout.height);
  return {
    centerX,
    centerZ,
    halfSpan: span / 2,
    radius: span * 0.36,
    span,
  };
}

/** Normalized disc coordinates used for stereographic projection. */
export function flatDiscCoords(
  flatX: number,
  flatZ: number,
  globe: GlobeLayout,
): { nx: number; nz: number; rho: number } {
  const nx = (flatX - globe.centerX) / globe.halfSpan;
  const nz = (flatZ - globe.centerZ) / globe.halfSpan;
  return { nx, nz, rho: Math.hypot(nx, nz) };
}

export function isInsideWorldDisc(flatX: number, flatZ: number, globe: GlobeLayout, margin = 1.001): boolean {
  return flatDiscCoords(flatX, flatZ, globe).rho <= margin;
}

/** Flat disc coordinates → northern hemisphere cap on a sphere. */
function projectDiscToSphere(flatX: number, flatZ: number, globe: GlobeLayout): GlobePoint {
  const { nx, nz, rho } = flatDiscCoords(flatX, flatZ, globe);

  // Stereographic: flat disc maps to a spherical cap; rim sits on the equator.
  const rhoClamped = Math.min(rho, 1);
  const azimuth = Math.atan2(nx, -nz);
  const lat = Math.PI / 2 - 2 * Math.atan(rhoClamped);
  const lon = azimuth;

  const cosLat = Math.cos(lat);
  const sinLat = Math.sin(lat);
  const sinLon = Math.sin(lon);
  const cosLon = Math.cos(lon);

  const normal = new THREE.Vector3(cosLat * sinLon, sinLat, cosLat * cosLon);
  const position = normal.clone().multiplyScalar(globe.radius);

  return { position, normal };
}

export function flatToGlobe(flatX: number, flatZ: number, globe: GlobeLayout): GlobePoint {
  return projectDiscToSphere(flatX, flatZ, globe);
}

const scratchAlongX = new THREE.Vector3();
const scratchAlongZ = new THREE.Vector3();
const scratchTangentX = new THREE.Vector3();
const scratchY = new THREE.Vector3();
const scratchZ = new THREE.Vector3();
const scratchBasis = new THREE.Matrix4();

export function flatToGlobeFrame(
  flatX: number,
  flatZ: number,
  globe: GlobeLayout,
  delta = Math.max(globe.span * 0.01, 2),
): GlobeFrame {
  const center = projectDiscToSphere(flatX, flatZ, globe);
  const alongX = projectDiscToSphere(flatX + delta, flatZ, globe);
  const alongZ = projectDiscToSphere(flatX, flatZ + delta, globe);

  scratchAlongX.copy(alongX.position).sub(center.position);
  scratchAlongZ.copy(alongZ.position).sub(center.position);

  scratchTangentX.copy(scratchAlongX);
  scratchTangentX.addScaledVector(center.normal, -scratchTangentX.dot(center.normal));
  if (scratchTangentX.lengthSq() < 1e-8) {
    scratchTangentX.set(1, 0, 0);
    scratchTangentX.addScaledVector(center.normal, -scratchTangentX.dot(center.normal));
  }
  scratchTangentX.normalize();

  return { ...center, tangentX: scratchTangentX.clone() };
}

export function orientHexOnGlobe(
  object: THREE.Object3D,
  normal: THREE.Vector3,
  tangentX: THREE.Vector3,
) {
  scratchY.copy(normal).normalize();
  scratchTangentX.copy(tangentX);
  scratchTangentX.addScaledVector(scratchY, -scratchTangentX.dot(scratchY));
  if (scratchTangentX.lengthSq() < 1e-8) {
    scratchTangentX.set(1, 0, 0);
    scratchTangentX.addScaledVector(scratchY, -scratchTangentX.dot(scratchY));
  }
  scratchTangentX.normalize();
  scratchZ.crossVectors(scratchTangentX, scratchY).normalize();
  scratchBasis.makeBasis(scratchTangentX, scratchZ, scratchY);
  object.quaternion.setFromRotationMatrix(scratchBasis);
}

export function orientToNormal(object: THREE.Object3D, normal: THREE.Vector3) {
  object.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), normal);
}

export function placeExtrudedOnGlobe(
  object: THREE.Object3D,
  surface: THREE.Vector3,
  normal: THREE.Vector3,
  extrude: number,
  centerOffset = 0.5,
  tangentX?: THREE.Vector3,
) {
  if (tangentX) {
    orientHexOnGlobe(object, normal, tangentX);
  } else {
    orientToNormal(object, normal);
  }
  object.position.copy(surface).addScaledVector(normal, extrude * centerOffset);
}

export function surfacePoint(surface: THREE.Vector3, normal: THREE.Vector3, lift: number): THREE.Vector3 {
  return surface.clone().addScaledVector(normal, lift);
}
