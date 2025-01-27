using PathDataGenerator;
using System.Collections.Immutable;
using System.Drawing;
using System.Numerics;

internal readonly record struct Area
{
    public IReadOnlyCollection<Zone> Zones { get; }
    public Vector2 Origin { get; }

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

    public int NumberOfSquares
    {
        get
        {
            var ret = 0;
            for (var zx = 0; zx < Size.Width; zx++)
            {
                for (var zy = 0; zy < Size.Height; zy++)
                {
                    var absx = Start.X + zx;
                    var absy = Start.Y + zy;

                    var found = Zones.Any(zn => zn.Location.X == absx && zn.Location.Y == absy);

                    if (!found)continue;

                    ret += (Utils.NUM_SQUARES * Utils.NUM_SQUARES);
                }
            }

            return ret;
        }
    }
    private Area(IEnumerable<Zone> Zones, Vector2 Origin)
    {
        this.Origin = Origin;
        this.Zones = Zones.ToImmutableArray();
    }

    internal static Vector2 GetZonePos(CellIndex index) => new(index.ZX * Zone.UNIT_SIZE, index.ZY * Zone.UNIT_SIZE);

    internal static Vector2 GetSquarePos(CellIndex index)
    {
        var zonePos = GetZonePos(index);
        return new Vector2(
            index.SX * Utils.SQUARE_SIZE + zonePos.X,
            index.SY * Utils.SQUARE_SIZE + zonePos.Y
        );
    }

    internal Vector2 GetCellPos(CellIndex index)
    {
        var squarePosition = GetSquarePos(index);
        var cellLocalPosition = new Vector2(
                (index.CX + 0.5f) * Utils.CELL_SIZE,
                (index.CY + 0.5f) * Utils.CELL_SIZE
            );

        return new Vector2(
                cellLocalPosition.X + squarePosition.X,
                cellLocalPosition.Y + squarePosition.Y
        );
    }

    public static async Task<Area> CreateAsync(string areaName)
    {
        var areaDescr = await Utils.GetAreaDescription(areaName);
        var zones = areaDescr.ZoneLocations.Select(loc =>
            Zone.Create(Path.Combine(Utils.TOPO_PATH, $"x{loc.X}y{loc.Y}.idx"),
            areaDescr
            ));

        return new Area(zones, areaDescr.Origin);
    }

    public static Area Create(string areaName)
    {
        var task = CreateAsync(areaName);
        task.Wait();
        return task.Result;
    }
}