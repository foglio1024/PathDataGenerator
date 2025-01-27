using PathDataGenerator.Models;
using System.Numerics;
using Vezel.Novadrop.Data;

namespace PathDataGenerator;

internal static class Utils
{
    public static int NUM_CELLS = 8;
    public static int NUM_SQUARES = 120;
    public static int CELLS_IN_ZONE => NUM_SQUARES * NUM_CELLS;
    public static float SQUARE_SIZE => Zone.UNIT_SIZE / (float)NUM_SQUARES;
    public static float CELL_SIZE => SQUARE_SIZE / (float)NUM_CELLS;

    //public static string DC_PATH = @"E:\MTERA\MT - The Dream\Client\S1Game\S1Data\DataCenter_Final_EUR.dat";
    public static string DC_PATH = @"E:\TERA_DEV\TERA_GF115\Client\S1Game\S1Data\DataCenter_Final_EUR.dat";
    public static string TOPO_PATH = @"E:\TERA_DEV\out\";
    public static string PATHDATA_PATH = @"E:\TERA_DEV\out\";

    public static float Mod(float a, float b) => (a % b + b) % b;

    internal static int GetCellIndex(int sx, int sy, int cx, int cy)
    {
        return cy + (sy * NUM_CELLS)
            + (cx * NUM_CELLS * NUM_SQUARES)
            + (sx * NUM_CELLS * NUM_CELLS * NUM_SQUARES);
    }

    internal static CellIndex GetCellIndexFromPoint(Vector2 point)
    {
        var zx = point.X / Zone.UNIT_SIZE;
        var zy = point.Y / Zone.UNIT_SIZE;

        var zoneLocalX = Utils.Mod(point.X, Zone.UNIT_SIZE);
        var zoneLocalY = Utils.Mod(point.Y, Zone.UNIT_SIZE);

        var (squareX, squareLocalX) = int.DivRem((int)zoneLocalX, (int)SQUARE_SIZE);
        var (squareY, squareLocalY) = int.DivRem((int)zoneLocalY, (int)SQUARE_SIZE);

        var cellX = squareLocalX / ((int)CELL_SIZE);
        var cellY = squareLocalY / ((int)CELL_SIZE);

        return new CellIndex((int)zx, (int)zy, squareX, squareY, cellX, cellY, -1);
    }

    internal static async Task<AreaDescription> GetAreaDescription(string areaName)
    {
        var dc = await DataCenter.LoadAsync(File.OpenRead(DC_PATH),
            new DataCenterLoadOptions()
            .WithLoaderMode(DataCenterLoaderMode.Transient)
            .WithMutability(DataCenterMutability.Immutable)
            //.WithKey("1c01c904ff76ff06c211187e197b5716".ToByteArrayHex()) // menma
            //.WithIV("396c342c52a0c12d511dd0209f90ca7d".ToByteArrayHex()) // menma
            );

        var area = dc.Children.Single(x => x.Name == "AreaList")
            .Descendants().First(x => x.Name == "Area"
                && x.Attributes["name"].AsString == areaName);

        var zones = area.Descendants().Where(x => x.Name == "Zone")
            .Select(z => new Vector2(z.Attributes["x"].AsInt32, z.Attributes["y"].AsInt32))
            ;


        var origin = new Vector2(area.Parent!.Attributes["originZoneX"].AsInt32,
                                 area.Parent!.Attributes["originZoneY"].AsInt32);

        return new AreaDescription(origin, zones.ToArray().AsReadOnly());
    }
}