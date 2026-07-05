namespace TTS.Core.Systems;

using TTS.Core.Models;

/// <summary>
/// Icosahedron subdivision → dual Goldberg polyhedron (mostly hexagons + 12 pentagons).
/// Each tile is a rigid face: center + normal + polygon ring + spherical neighbours.
/// </summary>
public static class GoldbergPolyhedronGenerator
{
    public sealed record TileMesh(
        string Id,
        SphereVec3 Center,
        SphereVec3 Normal,
        IReadOnlyList<SphereVec3> PolygonVertices,
        IReadOnlyList<string> NeighbourIds,
        bool IsPentagon);

    public static IReadOnlyList<TileMesh> Generate(int subdivisions, double planetRadius)
    {
        subdivisions = Math.Clamp(subdivisions, 1, 6);
        var (vertices, faces) = BuildSubdividedIcosahedron(subdivisions);
        return BuildDualTiles(vertices, faces, planetRadius);
    }

    private static (List<SphereVec3> Vertices, List<(int A, int B, int C)> Faces) BuildSubdividedIcosahedron(int subdivisions)
    {
        var vertices = CreateIcosahedronVertices();
        var faces = CreateIcosahedronFaces();

        for (var i = 0; i < subdivisions; i++)
        {
            var subdivided = Subdivide(vertices, faces);
            vertices = subdivided.Vertices;
            faces = subdivided.Faces;
        }

        for (var i = 0; i < vertices.Count; i++)
            vertices[i] = vertices[i].Normalized();

        return (vertices, faces);
    }

    private static List<SphereVec3> CreateIcosahedronVertices()
    {
        const double t = 1.618033988749895;
        (double X, double Y, double Z)[] raw =
        [
            (-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0),
            (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t),
            (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1),
        ];

        return raw.Select(v => new SphereVec3(v.X, v.Y, v.Z).Normalized()).ToList();
    }

    private static List<(int A, int B, int C)> CreateIcosahedronFaces() =>
    [
        (0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11),
        (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8),
        (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9),
        (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1),
    ];

    private static (List<SphereVec3> Vertices, List<(int A, int B, int C)> Faces) Subdivide(
        List<SphereVec3> vertices,
        List<(int A, int B, int C)> faces)
    {
        var nextVertices = new List<SphereVec3>(vertices);
        var midCache = new Dictionary<long, int>();
        var nextFaces = new List<(int A, int B, int C)>(faces.Count * 4);

        int Midpoint(int a, int b)
        {
            var key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (midCache.TryGetValue(key, out var existing))
                return existing;

            var mid = nextVertices[a].Add(nextVertices[b]).Scale(0.5).Normalized();
            var index = nextVertices.Count;
            nextVertices.Add(mid);
            midCache[key] = index;
            return index;
        }

        foreach (var (a, b, c) in faces)
        {
            var ab = Midpoint(a, b);
            var bc = Midpoint(b, c);
            var ca = Midpoint(c, a);
            nextFaces.Add((a, ab, ca));
            nextFaces.Add((ab, b, bc));
            nextFaces.Add((ca, bc, c));
            nextFaces.Add((ab, bc, ca));
        }

        return (nextVertices, nextFaces);
    }

    private static IReadOnlyList<TileMesh> BuildDualTiles(
        IReadOnlyList<SphereVec3> vertices,
        IReadOnlyList<(int A, int B, int C)> faces,
        double planetRadius)
    {
        var vertexFaces = new List<List<int>>(vertices.Count);
        for (var i = 0; i < vertices.Count; i++)
            vertexFaces.Add([]);

        var faceCentroids = new SphereVec3[faces.Count];
        for (var f = 0; f < faces.Count; f++)
        {
            var face = faces[f];
            faceCentroids[f] = vertices[face.A]
                .Add(vertices[face.B])
                .Add(vertices[face.C])
                .Scale(1.0 / 3.0)
                .Normalized()
                .Scale(planetRadius);

            vertexFaces[face.A].Add(f);
            vertexFaces[face.B].Add(f);
            vertexFaces[face.C].Add(f);
        }

        var tiles = new List<TileMesh>(vertices.Count);
        for (var v = 0; v < vertices.Count; v++)
        {
            var center = vertices[v].Normalized().Scale(planetRadius);
            var normal = center.Normalized();
            var orderedFaces = OrderFaceIndices(center, normal, vertexFaces[v], faceCentroids);
            var ring = orderedFaces.Select(f => faceCentroids[f]).ToList();
            var neighbourIds = new List<string>(orderedFaces.Count);
            for (var i = 0; i < orderedFaces.Count; i++)
            {
                var faceA = faces[orderedFaces[i]];
                var faceB = faces[orderedFaces[(i + 1) % orderedFaces.Count]];
                neighbourIds.Add(TileId(NeighborAcrossSharedEdge(v, faceA, faceB)));
            }

            tiles.Add(new TileMesh(
                TileId(v),
                center,
                normal,
                ring,
                neighbourIds,
                ring.Count == 5));
        }

        return tiles;
    }

    private static IReadOnlyList<int> OrderFaceIndices(
        SphereVec3 center,
        SphereVec3 normal,
        IReadOnlyList<int> faceIndices,
        IReadOnlyList<SphereVec3> faceCentroids)
    {
        var tangent = Math.Abs(normal.Y) < 0.95
            ? new SphereVec3(-normal.Z, 0, normal.X).Normalized()
            : new SphereVec3(0, -normal.Z, normal.Y).Normalized();
        var bitangent = normal.Cross(tangent).Normalized();

        return faceIndices
            .Select(f =>
            {
                var d = faceCentroids[f].Subtract(center);
                var tx = d.Dot(tangent);
                var ty = d.Dot(bitangent);
                return (Face: f, Angle: Math.Atan2(ty, tx));
            })
            .OrderBy(x => x.Angle)
            .Select(x => x.Face)
            .ToList();
    }

    private static int NeighborAcrossSharedEdge(int centerVertex, (int A, int B, int C) faceA, (int A, int B, int C) faceB)
    {
        foreach (var v in new[] { faceA.A, faceA.B, faceA.C })
        {
            if (v == centerVertex)
                continue;

            if (v == faceB.A || v == faceB.B || v == faceB.C)
                return v;
        }

        throw new InvalidOperationException("Adjacent faces around a vertex must share an edge.");
    }

    public static string TileId(int vertexIndex) => $"t{vertexIndex}";

    public static int ParseTileIndex(string tileId) =>
        int.Parse(tileId.AsSpan(1));
}

public readonly record struct SphereVec3(double X, double Y, double Z)
{
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    public SphereVec3 Normalized()
    {
        var len = Length;
        return len < 1e-12 ? this : new SphereVec3(X / len, Y / len, Z / len);
    }

    public SphereVec3 Add(SphereVec3 other) => new(X + other.X, Y + other.Y, Z + other.Z);
    public SphereVec3 Subtract(SphereVec3 other) => new(X - other.X, Y - other.Y, Z - other.Z);
    public SphereVec3 Scale(double s) => new(X * s, Y * s, Z * s);
    public double Dot(SphereVec3 other) => X * other.X + Y * other.Y + Z * other.Z;

    public SphereVec3 Cross(SphereVec3 other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);
}
