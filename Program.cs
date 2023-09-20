
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

var indexedVolumes = new Dictionary<int, IndexedVolume>();

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
                indexedVolumes[idx] = new IndexedVolume(new CellIndex(sx, sy, cx, cy), vols);
            }
        }
    }
}

var nodes = new Dictionary<int, Node>();

var total = indexedVolumes.Count;
var done = 0;
//Parallel.For(0, indexed.Count, new ParallelOptions { MaxDegreeOfParallelism = 8 }, idx =>
for (int idx = 0; idx < indexedVolumes.Count; idx++)
{
    var v = indexedVolumes[idx];

    var neighbors = new int[8];

    var left        = indexedVolumes.TryGetValue(v.Index.AddX(-1)           .ToIndex(), out var l)  && IsWalkable(v, l)     ? l.Index.ToIndex()  : -1;
    var bottomleft  = indexedVolumes.TryGetValue(v.Index.AddX(-1).AddY(1)   .ToIndex(), out var bl) && IsWalkable(v, bl)    ? bl.Index.ToIndex() : -1;
    var bottom      = indexedVolumes.TryGetValue(v.Index.AddY(1)            .ToIndex(), out var b)  && IsWalkable(v, b)     ? b.Index.ToIndex()  : -1;
    var bottomRight = indexedVolumes.TryGetValue(v.Index.AddY(1).AddX(1)    .ToIndex(), out var br) && IsWalkable(v, br)    ? br.Index.ToIndex() : -1;
    var right       = indexedVolumes.TryGetValue(v.Index.AddX(1)            .ToIndex(), out var r)  && IsWalkable(v, r)     ? r.Index.ToIndex()  : -1;
    var topright    = indexedVolumes.TryGetValue(v.Index.AddY(-1).AddX(1)   .ToIndex(), out var tr) && IsWalkable(v, tr)    ? tr.Index.ToIndex() : -1;
    var top         = indexedVolumes.TryGetValue(v.Index.AddY(-1)           .ToIndex(), out var t)  && IsWalkable(v, t)     ? t.Index.ToIndex()  : -1;
    var topleft     = indexedVolumes.TryGetValue(v.Index.AddY(-1).AddX(-1)  .ToIndex(), out var tl) && IsWalkable(v, tl)    ? tl.Index.ToIndex() : -1;

    neighbors[0] = left;
    neighbors[1] = bottomleft;
    neighbors[2] = bottom;
    neighbors[3] = bottomRight;
    neighbors[4] = right;
    neighbors[5] = topright;
    neighbors[6] = top;
    neighbors[7] = topleft;

    var distances = new int[8];
    var currCellPos = GetCellPos(v.Index);
    var currCellZ = v.Volumes[0].Z;  // only using 1st volume for now

    for (int i = 0; i < 8; i++)
    {
        var ni = neighbors[i];
        if (ni == -1)
        {
            distances[i] = int.MaxValue;
            continue;
        }

        var neigh = indexedVolumes[ni];
        // skip this if another node is already connected
        if (nodes.TryGetValue(ni, out var existing) && existing.Neighbors.Contains(idx))
        {
            neighbors[i] = -1;
            distances[i] = int.MaxValue;
            continue;
        }

        // get actual coords and calculate distance
        var npos = GetCellPos(neigh.Index);
        var neighCellZ = neigh.Volumes[0].Z;
        var dist = Math.Sqrt(
            Math.Pow(npos.X - currCellPos.X, 2) +
            Math.Pow(npos.Y - currCellPos.Y, 2) +
            Math.Pow(neighCellZ - currCellZ, 2)
            );

        distances[i] = Convert.ToInt32(dist);
    }

    var node = new Node(currCellPos.X, currCellPos.Y, currCellZ, neighbors, distances);
    nodes[idx] = node;

    Interlocked.Increment(ref done);
    if (done % 1000 == 0) Console.WriteLine($"{done}/{total} {done / (float)total:P1}");
}
//);

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

int GetCellIndex(int sx, int sy, int cx, int cy)
{
    return cy + (sy * NUM_CELLS) + (cx * NUM_CELLS * NUM_SQUARES) + (sx * NUM_CELLS * NUM_CELLS * NUM_SQUARES);
}

Volume? GetCellVolumeAt(IndexedVolume cell, int z, bool alsoSearchAbove)
{
    if (z == -16777215)
        return cell.Volumes.FirstOrDefault();

    return alsoSearchAbove
        ? cell.Volumes.MinBy(volume => int.Abs(volume.Z - z))
        : cell.Volumes.LastOrDefault(volume => volume.Z <= z);
}

Volume? CanGoNeighbourhoodCell(IndexedVolume current, IndexedVolume next, int z)
{
    var currentCell = current.Index;
    var nextCell = next.Index;
    
    if (int.Abs(currentCell.GetX() - nextCell.GetX()) > 1 ||
        int.Abs(currentCell.GetY() - nextCell.GetY()) > 1) 
        return null;
    
    var nextVolume = GetCellVolumeAt(next, z + 15, false);
    
    if (nextVolume == null)
        return null;

    if (z + 50 >= nextVolume.Value.Z + nextVolume.Value.Height)
        return null;

    if (z > nextVolume.Value.Z + 50)
        return null;

    return nextVolume;
}

bool IsWalkable(Point3D start, Point3D end)
{
    var heading = (end - start) with {Z = 0};
    var headingSquared = heading.Squared();

    if (Math.Abs(headingSquared - 1) < 0.01 || headingSquared > 1)
        heading /= float.Sqrt(headingSquared) * 15;

    var current = start;
    var next = start;
    var currentCellZ = (int) start.Z;

    var step = 0;
    while (current != end)
    {
        while (true)
        {
            if (step++ > 1000) return false;

            next = (end - current).Squared() >= 16 * 16
                ? next + heading
                : end;

            if (current.ToCellIndex() != next.ToCellIndex())
                break;

            current = next;
        }

        indexedVolumes.TryGetValue(current.ToCellIndex().ToIndex(), out var currentVolumes);
        indexedVolumes.TryGetValue(next.ToCellIndex().ToIndex(), out var nextVolumes);

        var nextVolume = CanGoNeighbourhoodCell(currentVolumes, nextVolumes, currentCellZ);
        if (nextVolume == null) break;

        currentCellZ = nextVolume.Value.Z;
        current = next;
    }

    var zDiff = float.Abs(end.Z - currentCellZ);
    return float.Abs(zDiff) <= 15;
}


readonly record struct IndexedVolume(CellIndex Index, Volume[] Volumes);

readonly record struct Zone(Square[,] Squares);

readonly record struct Square(Cell[,] Cells);

readonly record struct Cell(Volume[] Volumes);

readonly record struct Volume(short Z, ushort Height);

readonly record struct Point2D(float X, float Y);

readonly record struct Point3D(float X, float Y, float Z)
{
    public static Point3D operator +(Point3D a, Point3D b) =>
        new() { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z };

    public static Point3D operator -(Point3D a) =>
        new() { X = -a.X, Y = -a.Y, Z = -a.Z };

    public static Point3D operator -(Point3D a, Point3D b) => a + (-b);

    public static Point3D operator /(Point3D a, float b) =>         
        new() { X = a.X / b, Y = a.Y / b, Z = a.Z / b };

    public float Squared() => X * X + Y * Y + Z * Z;

    public CellIndex ToCellIndex() => new CellIndex()
        .AddX((int) (X / 16))
        .AddY((int) (Y / 16));
};

readonly record struct Node(float X, float Y, float Z, int[] Neighbors, int[] Distances);

readonly record struct CellIndex(int SX, int SY, int CX, int CY)
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

