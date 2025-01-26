﻿using System.Collections.ObjectModel;
using System.Numerics;
using Vezel.Novadrop.Data;

namespace PathDataGenerator;

static class Utils
{
    public static string DC_PATH = @"E:\TERA_DEV\TERA_GF115\Client\S1Game\S1Data\DataCenter_Final_EUR.dat";
    public static string TOPO_PATH = @"E:\TERA_DEV\out\";

    public static float Mod(float a, float b) => (a % b + b) % b;

    internal static int GetCellIndex(int sx, int sy, int cx, int cy)
    {
        return cy + (sy * Generator.NUM_CELLS)
            + (cx * Generator.NUM_CELLS * Generator.NUM_SQUARES)
            + (sx * Generator.NUM_CELLS * Generator.NUM_CELLS * Generator.NUM_SQUARES);
    }

    internal static CellIndex GetCellIndexFromPoint(Vector2 point)
    {
        var zx = point.X / Zone.UNIT_SIZE;
        var zy = point.Y / Zone.UNIT_SIZE;

        var zoneLocalX = Utils.Mod(point.X, Zone.UNIT_SIZE);
        var zoneLocalY = Utils.Mod(point.Y, Zone.UNIT_SIZE);

        var (squareX, squareLocalX) = int.DivRem((int)zoneLocalX, (int)Generator.SQUARE_SIZE);
        var (squareY, squareLocalY) = int.DivRem((int)zoneLocalY, (int)Generator.SQUARE_SIZE);

        var cellX = squareLocalX / ((int)Generator.CELL_SIZE * 2);
        var cellY = squareLocalY / ((int)Generator.CELL_SIZE * 2);

        return new CellIndex((int)zx, (int)zy, squareX, squareY, cellX, cellY, -1);
    }

    internal static async Task<AreaDescription> GetAreaDescription(string areaName)
    {
        var dc = await DataCenter.LoadAsync(File.OpenRead(DC_PATH),
            new DataCenterLoadOptions()
            .WithLoaderMode(DataCenterLoaderMode.Transient)
            .WithMutability(DataCenterMutability.Immutable)
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


readonly record struct AreaDescription(Vector2 Origin, ReadOnlyCollection<Vector2> ZoneLocations)
{

}