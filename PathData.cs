using PathDataGenerator;

class PathData
{
    public required int ZoneStartX { get; init; }
    public required int ZoneStartY { get; init; }
    public required int ZoneEndX { get; init; }
    public required int ZoneEndY { get; init; }
    public required List<Node> Nodes { get; init; }
    public required List<short> ZonesInSquare { get; init; }
    public required List<int> NodeIndices { get; init; }

    public static async Task<PathData> LoadAsync(string areaName, Area area)
    {
        var gdiPath = Path.Combine(Utils.PATHDATA_PATH, $"pathdata_{areaName}.gdi");

        await using var gdiFile = File.OpenRead(gdiPath);
        using var gdiReader = new BinaryReader(gdiFile);

        var zoneStartX = gdiReader.ReadInt32();
        var zoneStartY = gdiReader.ReadInt32();
        var zoneEndX = gdiReader.ReadInt32();
        var zoneEndY = gdiReader.ReadInt32();

        var totalNodes = gdiReader.ReadInt32();

        var zonesInSquare = new List<short>();

        //for (var i = 0; i < area.NumberOfSquares; i++)
        //{
        //    zonesInSquare.Add(gdiReader.ReadInt16());
        //}

        var indices = new List<int>();

        //for (var i = 0; i < totalNodes; i++)
        //{
        //    indices.Add(gdiReader.ReadInt32());
        //}

        var nodPath = Path.ChangeExtension(gdiPath, "nod");
        await using var nodFile = File.OpenRead(nodPath);
        using var nodReader = new BinaryReader(nodFile);

        var nodes = new List<Node>();

        for (int i = 0; i < totalNodes; i++)
        {
            var x = nodReader.ReadSingle();
            var y = nodReader.ReadSingle();
            var z = nodReader.ReadSingle();

            var neighbors = new int[8];
            var distances = new int[8];

            for (int n = 0; n < 8; n++)
            {
                neighbors[n] = nodReader.ReadInt32();
            }

            for (int d = 0; d < 8; d++)
            {
                distances[d] = nodReader.ReadInt32();
            }

            var node = new Node(x, y, z, neighbors, distances);

            nodes.Add(node);
        }

        return new PathData
        {
            NodeIndices = indices,
            Nodes = nodes,
            ZoneStartX = zoneStartX,
            ZoneStartY = zoneStartY,
            ZoneEndX = zoneEndX,
            ZoneEndY = zoneEndY,
            ZonesInSquare = zonesInSquare
        };
    }

}