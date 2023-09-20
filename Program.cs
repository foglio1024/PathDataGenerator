
const int NUM_SQUARES = 120;
const int NUM_CELLS = 8;
const float ZONE_SIZE = NUM_SQUARES * NUM_CELLS;//614.4f;

const float SQUARE_SIZE = ZONE_SIZE / (float)NUM_SQUARES;
const float CELL_SIZE = SQUARE_SIZE / (float)NUM_CELLS;

var idxFile = @"E:\TERA_DEV\x995y1007.idx";
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
                    var volume = cell.Volumes[vi] = new Volume
                    {
                        Z = geoReader.ReadUInt16(),
                        Height = geoReader.ReadUInt16(),
                    };
                }
            }
        }
    }
}

var volumes = new List<VolumePosition>();

Console.WriteLine("Zone loaded");

for (int sx = 0; sx < 120; sx++)
{
    for (int cx = 0; cx < 8; cx++)
    {
        for (int sy = 0; sy < 120; sy++)
        {
            for (int cy = 0; cy < 8; cy++)
            {
                var pos = GetCellPos(sx, sy, cx, cy);
                var vols = zone.Squares[sx, sy].Cells[cx, cy].Volumes;

                volumes.Add(new VolumePosition(vols, pos));
            }
        }
    }
}

var nodes = new List<Node>();
var total = volumes.Count;
var done = 0;
// Parallel.For(0,volumes.Count, idx =>
for (int idx = 0; idx < volumes.Count; idx++)
{
    var v = volumes[idx];

    var neighbors = new int[8];

    var left = volumes.FindIndex(x => x.Position.X == v.Position.X - CELL_SIZE && x.Position.Y == v.Position.Y);
    var bottomLeft = volumes.FindIndex(x => x.Position.X == v.Position.X - CELL_SIZE && x.Position.Y == v.Position.Y + CELL_SIZE);
    var bottom = volumes.FindIndex(x => x.Position.X == v.Position.X && x.Position.Y == v.Position.Y + CELL_SIZE);
    var bottomRight = volumes.FindIndex(x => x.Position.X == v.Position.X + CELL_SIZE && x.Position.Y == v.Position.Y + CELL_SIZE);
    var right = volumes.FindIndex(x => x.Position.X == v.Position.X + CELL_SIZE && x.Position.Y == v.Position.Y);
    var topRight = volumes.FindIndex(x => x.Position.X == v.Position.X + CELL_SIZE && x.Position.Y == v.Position.Y - CELL_SIZE);
    var top = volumes.FindIndex(x => x.Position.X == v.Position.X && x.Position.Y == v.Position.Y - CELL_SIZE);
    var topLeft = volumes.FindIndex(x => x.Position.X == v.Position.X - CELL_SIZE && x.Position.Y == v.Position.Y - CELL_SIZE);

    neighbors[0] = left;
    neighbors[1] = bottomLeft;
    neighbors[2] = bottom;
    neighbors[3] = bottomRight;
    neighbors[4] = right;
    neighbors[5] = topRight;
    neighbors[6] = top;
    neighbors[7] = topLeft;


    var distances = new int[8];

    for (int i = 0; i < 8; i++)
    {
        var ni = neighbors[i];
        if (ni == -1)
        {
            distances[i] = int.MaxValue;
            continue;
        }

        var n = volumes[ni];

        // don't add this neighbor if the other one already points to this
        // if (nodes.Any(node => node.Neighbors.Any(neigh => neigh == idx)))
        // {
        //     neighbors[i] = -1;
        //     distances[i] = int.MaxValue;
        //     continue;
        // }

        var dist = Math.Sqrt(
            Math.Pow(n.Position.X - v.Position.X, 2) +
            Math.Pow(n.Position.Y - v.Position.Y, 2) +
            Math.Pow(n.Volumes[0].Z - v.Volumes[0].Z, 2)
        );

        distances[i] = Convert.ToInt32(dist);
    }

    var node = new Node(v.Position.X, v.Position.Y, v.Volumes[0].Z, neighbors, distances); // only taking 1st volume for now

    nodes.Add(node);

    Interlocked.Increment(ref done);
    if (done % 1000 == 0) System.Console.WriteLine($"{done}/{total} {done / (float)total:P1}");
}
// );
var file = @"E:\TERA_DEV\pathdata_test.gdi";
using var gdi = new BinaryWriter(new BufferedStream(File.OpenWrite(file)));

var x1 = zoneX;
var y1 = zoneY;
var x2 = zoneX;
var y2 = zoneY;

gdi.Write(x1);
gdi.Write(y1);
gdi.Write(x2);
gdi.Write(y2);

gdi.Write(nodes.Count);

// todo: write the rest

using var nod = new BinaryWriter(new BufferedStream(File.OpenWrite(Path.ChangeExtension(file, "nod"))));

foreach (var node in nodes)
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


// var s = "";
// if (lt == default)
// {
//     s += " ";
// }
// else
// {
//     s += "•";
// }
// if (t == default)
// {
//     s += " ";
// }
// else
// {
//     s += "•";
// }
// if (rt == default)
// {
//     s += " ";
// }
// else
// {
//     s += "•";
// }
// s += "\n";
// if (l == default)
// {
//     s += " ";
// }
// else
// {
//     s += "•";
// }

// s += "x";

// if (r == default)
// {
//     s += " ";
// }
// else
// {
//     s += "•";
// }
// s += "\n";
// if (lb == default)
// {
//     s += " ";
// }
// else
// {
//     s += "•";
// }
// if (b == default)
// {
//     s += " ";
// }
// else
// {
//     s += "•";
// }
// if (rb == default)
// {
//     s += " ";
// }
// else
// {
//     s += "•";
// }

// System.Console.WriteLine(s);



Point2D GetSquarePos(int sx, int sy)
{
    return new Point2D(
        sx * SQUARE_SIZE + ZONE_REL_POS.X * ZONE_SIZE,
        sy * SQUARE_SIZE + ZONE_REL_POS.Y * ZONE_SIZE
    );
}

Point2D GetCellPos(int sx, int sy, int cx, int cy)
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
    return cy + (NUM_CELLS * cx) + (sy + sx * NUM_SQUARES) * (int)Math.Pow(NUM_CELLS, 2);
}

readonly record struct VolumePosition(Volume[] Volumes, Point2D Position);

readonly record struct Zone(Square[,] Squares);

readonly record struct Square(Cell[,] Cells);

readonly record struct Cell(Volume[] Volumes);

readonly record struct Volume(ushort Z, ushort Height);

readonly record struct Point2D(float X, float Y);
readonly record struct Node(float X, float Y, float Z, int[] Neighbors, int[] Distances);