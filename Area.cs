
using PathDataGenerator;
using System.Drawing;
using System.Numerics;

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

    internal static Vector2 GetZonePos(CellIndex index) => new(index.ZX * Zone.UNIT_SIZE, index.ZY * Zone.UNIT_SIZE);

    internal static Vector2 GetSquarePos(CellIndex index)
    {
        var zonePos = GetZonePos(index);
        return new Vector2(
            index.SX * Generator.SQUARE_SIZE + zonePos.X,
            index.SY * Generator.SQUARE_SIZE + zonePos.Y
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