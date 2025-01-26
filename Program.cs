﻿
using System.Diagnostics;
using PathDataGenerator;
using System.Numerics;

var sw = Stopwatch.StartNew();
var generator = new Generator("ATW_P");
Console.WriteLine("Created generator");
generator.GenerateNodes();
Console.WriteLine("Generated nodes");

//generator.Simplify();

generator.WriteNavdata("E:/TERA_DEV/out/");

sw.Stop();

Console.WriteLine($"Took {sw.Elapsed}");
readonly record struct IndexedVolume(CellIndex Index, Volume Volume);

readonly record struct Zone(Square[,] Squares, Vector2 Location, Vector2 Origin)
{
    public const float UNIT_SIZE = 15360;//614.4f;
}

readonly record struct Square(Cell[,] Cells);

readonly record struct Cell(Volume[] Volumes);

readonly record struct Volume(short Z, ushort Height);

readonly record struct Node(float X, float Y, float Z, int[] Neighbors, int[] Distances);

readonly record struct CellIndex(int ZX = -1, int ZY = -1, int SX = -1, int SY = -1, int CX = -1, int CY = -1, int VolumeIdx = -1)
{
    public CellIndex AddX(int x)
    {
        if (CX + x >= Generator.NUM_CELLS)
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
        return ZX * Generator.CELLS_IN_ZONE + SX * Generator.NUM_CELLS + CX;
    }

    public int GetY()
    {
        return ZY * Generator.CELLS_IN_ZONE + SY * Generator.NUM_CELLS + CY;
    }
}