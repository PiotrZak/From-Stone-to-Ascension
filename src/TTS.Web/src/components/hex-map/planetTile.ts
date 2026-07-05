/**
 * Target planet tile contract (backend-generated Goldberg / icosahedral dual mesh).
 *
 * The renderer must NOT derive spherical topology from lat/lon or a flat grid.
 * Until the API serves this shape, the flat-grid prototype in createThreeHexMap
 * is temporary and will show topological gaps near poles.
 */
export interface PlanetTileMesh {
  id: string;
  /** Unit-sphere normal × planet radius. */
  center: [number, number, number];
  /** Outward unit normal at tile center. */
  normal: [number, number, number];
  /** CCW polygon ring on the tangent plane (mostly hex, 12 pentagons on Goldberg). */
  polygonVertices: [number, number, number][];
  neighbourIds: string[];
  biome: string;
  terrain: string;
  elevation: number;
  resourceYield: number;
  controllingCivilizationId: string | null;
  isCapital: boolean;
}

export interface PlanetMap {
  planetRadius: number;
  tiles: PlanetTileMesh[];
}

/** Renderer contract: rigid extrusion along normal — never warp vertices. */
export type PlanetTileRenderInput = Pick<
  PlanetTileMesh,
  'center' | 'normal' | 'polygonVertices' | 'biome' | 'elevation'
>;
