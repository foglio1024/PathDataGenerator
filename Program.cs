
using PathDataGenerator;
using System.Drawing;
using System.Numerics;


var generator = new Generator("EX_SSC_A_CE_P");

generator.GenerateNodes();

generator.WriteNavdata("E:/TERA_DEV/out");

readonly record struct IndexedVolume(CellIndex Index, Volume Volume);

readonly record struct Zone(Square[,] Squares, Vector2 Location, Vector2 Origin)
{
    public Vector2 RelativeLocation => Location - Origin;
    public const float ZONE_SIZE = 15360;//614.4f;
}

readonly record struct Square(Cell[,] Cells);

readonly record struct Cell(Volume[] Volumes);

readonly record struct Volume(short Z, ushort Height);

readonly record struct Node(float X, float Y, float Z, int[] Neighbors, int[] Distances);

readonly record struct CellIndex(int ZX, int ZY, int SX, int SY, int CX, int CY, int VolumeIdx)
{
    public CellIndex AddX(int x)
    {
        if (CX + x >= 8)
        {
            if (SX + 1 >= Generator.NUM_SQUARES)
            {
                return this with { ZX = ZX + 1, SX = 0, CX = 0 };
            }
            else
            {

                return this with { SX = SX + 1, CX = 0 };
            }

        }
        else if (CX + x < 0)
        {
            if (SX - 1 < 0)
            {
                return this with { ZX = ZX - 1, SX = Generator.NUM_SQUARES - 1, CX = Generator.NUM_CELLS - 1 };
            }
            else
            {
                return this with { SX = SX - 1, CX = Generator.NUM_CELLS - 1 };
            }
        }
        return this with { CX = CX + x };

    }

    public CellIndex AddY(int y)
    {
        if (CY + y >= Generator.NUM_CELLS)
        {
            if (SY + 1 >= Generator.NUM_SQUARES)
            {
                return this with { ZY = ZY + 1, SY = 0, CY = 0 };

            }
            else
            {
                return this with { SY = SY + 1, CY = 0 };
            }
        }
        else if (CY + y < 0)
        {
            if (SY - 1 < 0)
            {
                return this with { ZY = ZX - 1, SY = Generator.NUM_SQUARES - 1, CY = Generator.NUM_CELLS - 1 };

            }
            else
            {
                return this with { SY = SY - 1, CY = Generator.NUM_CELLS - 1 };
            }
        }

        return this with { CY = CY + y };
    }

    public int GetX()
    {
        return ZX * (Generator.NUM_CELLS * Generator.NUM_SQUARES) + SX * Generator.NUM_CELLS + CX;
    }

    public int GetY()
    {
        return ZY * (Generator.NUM_CELLS * Generator.NUM_SQUARES) + SY * Generator.NUM_CELLS + CY;
    }
}

readonly record struct Area(Zone[] Zones, Vector2 Origin)
{
    public Vector2 Start
    {
        get
        {
            var minx = float.MaxValue;
            var miny = float.MaxValue;

            foreach (var z in Zones)
            {
                if (z.Location.X < minx) minx = z.Location.X;
                if (z.Location.Y < miny) miny = z.Location.Y;
            }

            return new Vector2(minx, miny);
        }
    }

    public Vector2 End
    {
        get
        {
            var maxx = float.MinValue;
            var maxy = float.MinValue;

            foreach (var z in Zones)
            {
                if (z.Location.X > maxx) maxx = z.Location.X;
                if (z.Location.Y > maxy) maxy = z.Location.Y;
            }

            return new Vector2(maxx, maxy);
        }
    }

    public Size Size => new((int)Math.Abs(End.X - Start.X) + 1, (int)Math.Abs(End.Y - Start.Y) + 1);


    internal Vector2 GetZonePos(CellIndex index)
    {
        // todo: consider zone
        return new Vector2(
            index.ZX * Zone.ZONE_SIZE,
            index.ZY * Zone.ZONE_SIZE
        );
    }

    internal Vector2 GetSquarePos(CellIndex index)
    {
        var zonePos = GetZonePos(index);
        return new Vector2(
            index.SX * Generator.SQUARE_SIZE + /*zone.RelativeLocation*/zonePos.X /** Zone.ZONE_SIZE*/,
            index.SY * Generator.SQUARE_SIZE + /*zone.RelativeLocation*/zonePos.Y /** Zone.ZONE_SIZE*/
        );
    }

    internal Vector2 GetCellPos(CellIndex index)
    {
        var squarePosition = GetSquarePos(index);
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