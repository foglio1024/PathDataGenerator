
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

const int NUM_SQUARES = 120;
const int NUM_CELLS = 8;
const float ZONE_SIZE = 15360;//614.4f;

const float SQUARE_SIZE = ZONE_SIZE / (float)NUM_SQUARES;
const float CELL_SIZE = SQUARE_SIZE / (float)NUM_CELLS;

var idxFile = @"E:\TERA_DEV\Server\Topology\x993y1008.idx";
var geoFile = Path.ChangeExtension(idxFile, "geo");

var zoneXY = Path.GetFileNameWithoutExtension(idxFile).Replace("x", "").Replace("y", " ").Split();

int zoneX = int.Parse(zoneXY[0]);
int zoneY = int.Parse(zoneXY[1]);
int originX = 1000;
int originY = 1000;

Point2D ZONE_REL_POS = new Point2D(zoneX - originX, zoneY - originY);

var zone = new Zone
{
    Squares = new Square[120, 120]
};

var idxReader = new BinaryReader(new BufferedStream(File.OpenRead(idxFile)));
var geoReader = new BinaryReader(new BufferedStream(File.OpenRead(geoFile)));

for (var sx = 0; sx < zone.Squares.GetLength(0); sx++)
{
    for (var sy = 0; sy < zone.Squares.GetLength(1); sy++)
    {
        var square = zone.Squares[sx, sy] = new Square
        {
            Cells = new Cell[8, 8],
        };

        _ = idxReader.ReadInt32();

        for (var cx = 0; cx < square.Cells.GetLength(0); cx++)
        {
            for (var cy = 0; cy < square.Cells.GetLength(1); cy++)
            {
                var cell = square.Cells[cx, cy] = new Cell
                {
                    Volumes = new Volume[idxReader.ReadUInt16()],
                };

                for (var vi = 0; vi < cell.Volumes.Length; vi++)
                {
                    var rawz = geoReader.ReadUInt16();
                    var z = rawz > short.MaxValue ? rawz - ushort.MaxValue : rawz;
                    cell.Volumes[vi] = new Volume
                    {
                        Z = (short)z,
                        Height = geoReader.ReadUInt16(),
                    };
                }
            }
        }
    }
}

var indexedVolumes = new Dictionary<CellIndex, IndexedVolume>();

Console.WriteLine("Zone loaded");

for (int sx = 0; sx < 120; sx++)
{
    for (int sy = 0; sy < 120; sy++)
    {
        for (int cx = 0; cx < 8; cx++)
        {
            for (int cy = 0; cy < 8; cy++)
            {
                var vols = zone.Squares[sx, sy].Cells[cx, cy].Volumes;
                var idx = GetCellIndex(sx, sy, cx, cy);

                for (int vidx = 0; vidx < vols.Length; vidx++)
                {
                    var cidx = new CellIndex(sx, sy, cx, cy, vidx);
                    indexedVolumes[cidx] = new IndexedVolume(cidx, vols[vidx]);
                }

            }
        }
    }
}

var nodes = new ConcurrentDictionary<int, Node>();

var total = indexedVolumes.Count;
var done = 0;

List<IndexedVolume> GetIndexedVolumesAtCell(CellIndex indexedVolume)
{
    var list = new List<IndexedVolume>();
    int searchIdx = 0;
    while (indexedVolumes.TryGetValue(indexedVolume with { VolumeIdx = searchIdx }, out var neigh))
    {
        list.Add(neigh);
        searchIdx++;
    }

    return list;
}

var volumesArray = indexedVolumes.Values.ToArray();
Parallel.For(0, indexedVolumes.Count, new ParallelOptions { MaxDegreeOfParallelism = 8 }, idx =>
//for (int idx = 0; idx < indexedVolumes.Count; idx++)
{
    var v = volumesArray[idx];

    var neighbourCells = new[]
    {
        v.Index.AddX(-1),
        v.Index.AddX(-1).AddY(1),
        v.Index.AddY(1),
        v.Index.AddY(1).AddX(1),
        v.Index.AddX(1),
        v.Index.AddY(-1).AddX(1),
        v.Index.AddY(-1),
        v.Index.AddY(-1).AddX(-1),
    };

    var neighbourIndexedVolumes =
        neighbourCells.Select(cell => indexedVolumes.TryGetValue(cell, out var neigh) ? neigh : (IndexedVolume?)null).ToArray();

    //for (var vi = 0; vi < v.Volumes.Length; vi++)
    {
        var volume = v.Volume;
        var neighbors = new int[8];
        var distances = new int[8];
        var currCellPos = GetCellPos(v.Index).ToPoint3D() with { Z = volume.Z };

        for (int i = 0; i < 8; i++)
        {
            var niv = neighbourIndexedVolumes[i];
            if (niv == null)
            {
                neighbors[i] = -1;
                distances[i] = int.MaxValue;
                continue;
            }

            //var neigh = niv.Value;
            var neighs = GetIndexedVolumesAtCell(niv.Value.Index);
            //var neighs = indexedVolumes.Keys.Where(k2 => k2 with { VolumeIdx = -1 } == niv.Value.Index with { VolumeIdx = -1 }).ToArray()
                            //.Select(key => indexedVolumes[key]).ToArray();

            var neighVolume = GetCellVolumeAt(neighs.ToArray(), volume.Z + 15, false);

            var neighCellPos = GetCellPos(niv.Value.Index).ToPoint3D();

            if (neighVolume == null || !IsWalkable(currCellPos, neighCellPos with { Z = neighVolume.Value.Volume.Z }))
            {
                neighbors[i] = -1;
                distances[i] = int.MaxValue;
                continue;
            }

            neighCellPos = neighCellPos with { Z = neighVolume.Value.Volume.Z };

            // get actual coords and calculate distance
            var dist = Math.Sqrt((neighCellPos - currCellPos).Squared());

            // TODO: figure out indices
            neighbors[i] = Array.IndexOf(volumesArray, neighVolume); //idx; //(idx, Array.IndexOf(neigh.Volumes, volume));
            distances[i] = Convert.ToInt32(dist);
        }

        var node = new Node(currCellPos.X, currCellPos.Y, currCellPos.Z, neighbors, distances);
        nodes[idx] = node;
    }

    Interlocked.Increment(ref done);
    if (done % 1000 == 0) Console.WriteLine($"{done}/{total} {done / (float)total:P1}");
}
);

// --- //

var ouputPath = @"E:\TERA_DEV\pathdata_test.gdi";

using var gdi = new BinaryWriter(new BufferedStream(File.OpenWrite(ouputPath)));

// todo: get actual start/end
var x1 = zoneX;
var y1 = zoneY;
var x2 = zoneX;
var y2 = zoneY;

gdi.Write(x1);
gdi.Write(y1);
gdi.Write(x2);
gdi.Write(y2);

gdi.Write(nodes.Count);

// todo: write the rest of the arrays


// --- //
using var nod = new BinaryWriter(new BufferedStream(File.OpenWrite(Path.ChangeExtension(ouputPath, "nod"))));

foreach (var node in nodes.Values)
{
    nod.Write(node.X);
    nod.Write(node.Y);
    nod.Write(node.Z);

    foreach (var nidx in node.Neighbors)
    {
        // TODO: recalculate node index before this
        nod.Write(nidx);
    }

    foreach (var dist in node.Distances)
    {
        nod.Write(dist);
    }
}


// --- //

Point2D GetSquarePos(int sx, int sy)
{
    return new Point2D(
        sx * SQUARE_SIZE + ZONE_REL_POS.X * ZONE_SIZE,
        sy * SQUARE_SIZE + ZONE_REL_POS.Y * ZONE_SIZE
    );
}

Point2D GetCellPos(CellIndex index)
{
    return GetCellPosEx(index.SX, index.SY, index.CX, index.CY);
}

Point2D GetCellPosEx(int sx, int sy, int cx, int cy)
{
    var sq_pos = GetSquarePos(sx, sy);
    var cell_rel_pos = new Point2D(
            (cx + 0.5f) * (SQUARE_SIZE / (float)NUM_CELLS),
            (cy + 0.5f) * (SQUARE_SIZE / (float)NUM_CELLS)
        );

    return new Point2D(
            cell_rel_pos.X + sq_pos.X,
            cell_rel_pos.Y + sq_pos.Y
    );
}

// apparently there is no modulo???
float Mod(float a, float b) => (a % b + b) % b;

CellIndex GetCellIndexFromPoint(Point2D point)
{
    var zoneLocalX = Mod(point.X, ZONE_SIZE);
    var zoneLocalY = Mod(point.Y, ZONE_SIZE);

    var (squareX, squareLocalX) = int.DivRem((int) zoneLocalX, (int) SQUARE_SIZE);
    var (squareY, squareLocalY) = int.DivRem((int) zoneLocalY, (int) SQUARE_SIZE);

    var cellX = squareLocalX / 16;
    var cellY = squareLocalY / 16;
    
    return new CellIndex(squareX, squareY, cellX, cellY, -1);
}

int GetCellIndex(int sx, int sy, int cx, int cy)
{
    return cy + (sy * NUM_CELLS) + (cx * NUM_CELLS * NUM_SQUARES) + (sx * NUM_CELLS * NUM_CELLS * NUM_SQUARES);
}

IndexedVolume? GetCellVolumeAt(IEnumerable<IndexedVolume> cell, int z, bool alsoSearchAbove)
{
    if (z == -16777215)
        return cell.FirstOrDefault();

    return alsoSearchAbove
        ? cell.MinBy(idx => int.Abs(idx.Volume.Z - z))
        : cell.LastOrDefault(idx => idx.Volume.Z <= z);
}

Volume? CanGoNeighbourhoodCell(CellIndex current, CellIndex next, int z)
{
    if (int.Abs(current.GetX() - next.GetX()) > 1 ||
        int.Abs(current.GetY() - next.GetY()) > 1)
        return null;

    var allNextVolumes = GetIndexedVolumesAtCell(next);
    var nextVolume = GetCellVolumeAt(allNextVolumes, z + 15, false);

    if (nextVolume == null)
        return null;

    if (z + 50 >= nextVolume.Value.Volume.Z + nextVolume.Value.Volume.Height)
        return null;

    if (z > nextVolume.Value.Volume.Z + 50)
        return null;

    return nextVolume.Value.Volume;
}

bool IsWalkable(Point3D start, Point3D end)
{
    var heading = (end - start) with { Z = 0 };
    var headingSquared = heading.Squared();

    if (Math.Abs(headingSquared - 1) < 0.01 || headingSquared > 1)
        heading = heading / float.Sqrt(headingSquared) * 15;

    var current = start;
    var next = start;
    var currentCellZ = (int)start.Z;

    var step = 0;
    while (true)
    {
        while (true)
        {
            if (step++ > 1000) return false;
            if (current == end)
            {
                var zDiff = float.Abs(end.Z - currentCellZ);
                return float.Abs(zDiff) <= 15;
            }
            
            next = (end - current).Squared() >= 16 * 16
                ? next + heading
                : end;

            if (GetCellIndexFromPoint(current.ToPoint2D()) != GetCellIndexFromPoint(next.ToPoint2D()))
                break;

            current = next;
        }

        var currentCell = GetCellIndexFromPoint(current.ToPoint2D());
        var nextCell = GetCellIndexFromPoint(next.ToPoint2D());

        var nextVolume = CanGoNeighbourhoodCell(currentCell, nextCell, currentCellZ);
        if (nextVolume == null) return false;

        currentCellZ = nextVolume.Value.Z;
        current = next;
    }
}

readonly record struct IndexedVolume(CellIndex Index, Volume Volume);

readonly record struct Zone(Square[,] Squares);

readonly record struct Square(Cell[,] Cells);

readonly record struct Cell(Volume[] Volumes);

readonly record struct Volume(short Z, ushort Height);

readonly record struct Point2D(float X, float Y)
{
    public Point3D ToPoint3D() => new() { X = X, Y = Y, Z = 0 };
};

readonly record struct Point3D(float X, float Y, float Z)
{
    public static Point3D operator +(Point3D a, Point3D b) =>
        new() { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z };

    public static Point3D operator -(Point3D a) =>
        new() { X = -a.X, Y = -a.Y, Z = -a.Z };

    public static Point3D operator -(Point3D a, Point3D b) => a + (-b);

    public static Point3D operator *(Point3D a, float b) =>
        new() { X = a.X * b, Y = a.Y * b, Z = a.Z * b };

    public static Point3D operator /(Point3D a, float b) =>
        new() { X = a.X / b, Y = a.Y / b, Z = a.Z / b };

    public float Squared() => X * X + Y * Y + Z * Z;
    
    public Point2D ToPoint2D() => new() { X = X, Y = Y };
};

readonly record struct Node(float X, float Y, float Z, int[] Neighbors, int[] Distances);

readonly record struct CellIndex(int SX, int SY, int CX, int CY, int VolumeIdx)
{
    public CellIndex AddX(int x)
    {
        if (CX + x >= 8)
        {
            return this with { SX = SX + 1, CX = 0 };

        }
        else if (CX + x < 0)
        {
            return this with { SX = SX - 1, CX = 7 };
        }
        return this with { CX = CX + x };

    }

    public CellIndex AddY(int y)
    {
        if (CY + y >= 8)
        {
            return this with { SY = SY + 1, CY = 0 };
        }
        else if (CY + y < 0)
        {
            return this with { SY = SY - 1, CY = 7 };
        }

        return this with { CY = CY + y };
    }

    public int GetX()
    {
        return SX * 8 + CX;
    }

    public int GetY()
    {
        return SY * 8 + CY;
    }

    public int ToIndex()
    {
        return GetY() + 120 * GetX();
    }
}

