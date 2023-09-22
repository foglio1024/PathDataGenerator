
using PathDataGenerator;
using System.Numerics;


var idxFile = @"E:\TERA_DEV\Server\Topology\x993y1008.idx";

var generator = new Generator("Rucmia_P");

generator.LoadZone(idxFile);

generator.GenerateNodes();

generator.WriteNavdata("E:/TERA_DEV/out");

readonly record struct IndexedVolume(CellIndex Index, Volume Volume);

readonly record struct Zone(Square[,] Squares, Vector2 Location, Vector2 Origin)
{
    public Vector2 RelativeLocation => Location - Origin;
    public const float ZONE_SIZE = 15360;//614.4f;

    internal Vector2 GetSquarePos(int sx, int sy)
    {
        return new Vector2(
            sx * Generator.SQUARE_SIZE + RelativeLocation.X * ZONE_SIZE,
            sy * Generator.SQUARE_SIZE + RelativeLocation.Y * ZONE_SIZE
        );
    }

    internal Vector2 GetCellPos(CellIndex index)
    {
        var squarePosition = GetSquarePos(index.SX, index.SY);
        var cellLocalPosition = new Vector2(
                (index.CX + 0.5f) * Generator.CELL_SIZE,
                (index.CY + 0.5f) * Generator.CELL_SIZE
            );

        return new Vector2(
                cellLocalPosition.X + squarePosition.X,
                cellLocalPosition.Y + squarePosition.Y
        );
    }
}

readonly record struct Square(Cell[,] Cells);

readonly record struct Cell(Volume[] Volumes);

readonly record struct Volume(short Z, ushort Height);

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
        if (CY + y >= Generator.NUM_CELLS)
        {
            return this with { SY = SY + 1, CY = 0 };
        }
        else if (CY + y < 0)
        {
            return this with { SY = SY - 1, CY = Generator.NUM_CELLS - 1 };
        }

        return this with { CY = CY + y };
    }

    public int GetX()
    {
        return SX * Generator.NUM_CELLS + CX;
    }

    public int GetY()
    {
        return SY * Generator.NUM_CELLS + CY;
    }

    public int ToIndex()
    {
        return GetY() + Generator.NUM_SQUARES * GetX();
    }
}

readonly record struct Area(Zone[] Zones)
{

}