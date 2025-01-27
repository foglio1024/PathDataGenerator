using PathDataGenerator;

internal readonly record struct CellIndex(int ZX = -1, int ZY = -1, int SX = -1, int SY = -1, int CX = -1, int CY = -1, int VolumeIdx = -1)
{
    public CellIndex AddX(int x)
    {
        if (CX + x >= Utils.NUM_CELLS)
        {
            if (SX + 1 >= Utils.NUM_SQUARES)
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
                return this with { ZX = ZX - 1, SX = Utils.NUM_SQUARES - 1, CX = Utils.NUM_CELLS - 1 };
            }
            else
            {
                return this with { SX = SX - 1, CX = Utils.NUM_CELLS - 1 };
            }
        }
        return this with { CX = CX + x };

    }

    public CellIndex AddY(int y)
    {
        if (CY + y >= Utils.NUM_CELLS)
        {
            if (SY + 1 >= Utils.NUM_SQUARES)
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
                return this with { ZY = ZX - 1, SY = Utils.NUM_SQUARES - 1, CY = Utils.NUM_CELLS - 1 };

            }
            else
            {
                return this with { SY = SY - 1, CY = Utils.NUM_CELLS - 1 };
            }
        }

        return this with { CY = CY + y };
    }

    public int GetX()
    {
        return ZX * Utils.CELLS_IN_ZONE + SX * Utils.NUM_CELLS + CX;
    }

    public int GetY()
    {
        return ZY * Utils.CELLS_IN_ZONE + SY * Utils.NUM_CELLS + CY;
    }
}