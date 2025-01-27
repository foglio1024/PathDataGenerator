using System.Numerics;
using PathDataGenerator;
using PathDataGenerator.Models;

internal readonly record struct Zone(Square[,] Squares, Vector2 Location, Vector2 Origin)
{
    public const float UNIT_SIZE = 15360;//614.4f;

    public static Zone Create(string idxFilePath, AreaDescription areaDescr)
    {
        var zoneXY = Path.GetFileNameWithoutExtension(idxFilePath)
                         .Replace("x", "").Replace("y", " ").Split();

        var zoneX = int.Parse(zoneXY[0]);
        var zoneY = int.Parse(zoneXY[1]);

        var geoFilePath = Path.ChangeExtension(idxFilePath, "geo");

        var zone = new Zone
        {
            Squares = new Square[120, 120],
            Location = new Vector2(zoneX, zoneY),
            Origin = areaDescr.Origin,
        };

        var idxReader = new BinaryReader(new BufferedStream(File.OpenRead(idxFilePath)));
        var geoReader = new BinaryReader(new BufferedStream(File.OpenRead(geoFilePath)));

        for (var sx = 0; sx < zone.Squares.GetLength(0); sx++)
        {
            for (var sy = 0; sy < zone.Squares.GetLength(1); sy++)
            {
                var square = zone.Squares[sx, sy] = new Square
                {
                    Cells = new Cell[Utils.NUM_CELLS, Utils.NUM_CELLS],
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

        Console.WriteLine($"Loaded zone ({zoneX},{zoneY}) from {idxFilePath}");

        return zone;
    }

}