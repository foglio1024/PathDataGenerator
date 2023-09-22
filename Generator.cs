using System.Collections.Concurrent;
using System.Numerics;

namespace PathDataGenerator;

class Generator
{
    public const int NUM_SQUARES = 120;
    public const int NUM_CELLS = 8;
    public const float SQUARE_SIZE = Zone.ZONE_SIZE / (float)NUM_SQUARES;
    public const float CELL_SIZE = SQUARE_SIZE / (float)NUM_CELLS;

    Indexer? _indexer;
    Zone _zone;
    //List<Zone> _zones;
    ConcurrentDictionary<int, Node>? _nodes;
    string _areaName;
    Vector2 _origin;

    public Generator(string areaName)
    {
        _zone = new();

        _areaName = areaName;

        //var areaDescrTask = Utils.GetAreaDescription(areaName);
        //areaDescrTask.Wait();
        //var areaDescr = areaDescrTask.Result;

        //_origin = areaDescr.Origin;

        //foreach (var zoneLocation in areaDescr.ZoneLocations)
        //{
        //    var zone = LoadZone(Path.Combine(Utils.TOPO_PATH, $"x{zoneLocation.X}y{zoneLocation.X}.idx"));
        //    _zones.Add(zone);
        //}

        //_indexer = new Indexer(zone);

    }

    public Zone LoadZone(string idxFilePath)
    {
        var zoneXY = Path.GetFileNameWithoutExtension(idxFilePath)
                         .Replace("x", "").Replace("y", " ").Split();

        int zoneX = int.Parse(zoneXY[0]);
        int zoneY = int.Parse(zoneXY[1]);

        var geoFilePath = Path.ChangeExtension(idxFilePath, "geo");

        var zone = new Zone
        {
            Squares = new Square[120, 120],
            Location = new Vector2(zoneX, zoneY),
            Origin = _origin,
        };

        var idxReader = new BinaryReader(new BufferedStream(File.OpenRead(idxFilePath)));
        var geoReader = new BinaryReader(new BufferedStream(File.OpenRead(geoFilePath)));

        for (var sx = 0; sx < zone.Squares.GetLength(0); sx++)
        {
            for (var sy = 0; sy < zone.Squares.GetLength(1); sy++)
            {
                var square = zone.Squares[sx, sy] = new Square
                {
                    Cells = new Cell[8, 8],
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

        Console.WriteLine("Zone loaded");
        _indexer = new Indexer(zone);
        _zone = zone;
        return zone;
    }

    public ConcurrentDictionary<int, Node> GenerateNodes()
    {
        var indexedVolumes = _indexer.IndexedVolumes;
        var volumesArray = _indexer.VolumesArray;
        var volumeIndices = _indexer.VolumeIndices;

        var total = indexedVolumes.Count;
        var done = 0;

        var nodes = new ConcurrentDictionary<int, Node>();
        Parallel.For(0, indexedVolumes.Count, new ParallelOptions { MaxDegreeOfParallelism = 8 }, idx =>
        {
            var v = volumesArray[idx];

            var neighbourCells = new[]
            {
                v.Index.AddX(-1),
                v.Index.AddX(-1).AddY(1),
                v.Index.AddY(1),
                v.Index.AddY(1).AddX(1),
                v.Index.AddX(1),
                v.Index.AddY(-1).AddX(1),
                v.Index.AddY(-1),
                v.Index.AddY(-1).AddX(-1),
            };

            var neighbourIndexedVolumes =
                neighbourCells.Select(cell => indexedVolumes.TryGetValue(cell, out var neigh)
                    ? neigh
                    : (IndexedVolume?)null).ToArray();

            var volume = v.Volume;
            var neighbors = new int[8];
            var distances = new int[8];
            var currCellPos = _zone.GetCellPos(v.Index).ToVector3() with { Z = volume.Z };

            for (int i = 0; i < 8; i++)
            {
                var niv = neighbourIndexedVolumes[i];
                if (niv == null)
                {
                    neighbors[i] = -1;
                    distances[i] = int.MaxValue;
                    continue;
                }

                var neighs = GetIndexedVolumesAtCell(niv.Value.Index);

                var neighVolume = GetCellVolumeAt(neighs.ToArray(), volume.Z + 15, false);

                var neighCellPos = _zone.GetCellPos(niv.Value.Index).ToVector3();

                if (neighVolume == null
                || !IsWalkable(currCellPos, neighCellPos with { Z = neighVolume.Value.Volume.Z }))
                {
                    neighbors[i] = -1;
                    distances[i] = int.MaxValue;
                    continue;
                }

                neighCellPos = neighCellPos with { Z = neighVolume.Value.Volume.Z };

                var dist = Vector3.Distance(neighCellPos, currCellPos);

                neighbors[i] = volumeIndices[neighVolume.Value.Index];
                distances[i] = Convert.ToInt32(dist);
            }

            var node = new Node(currCellPos.X, currCellPos.Y, currCellPos.Z, neighbors, distances);
            nodes[idx] = node;

            Interlocked.Increment(ref done);
            if (done % 1000 == 0) Console.WriteLine($"{done}/{total} {done / (float)total:P1}");
        }
        );

        _nodes = nodes;
        return nodes;
    }

    public void WriteNavdata(string outputFolder)
    {
        var gdiPath = Path.Combine(outputFolder, $"pathdata_{_areaName}.gdi");

        using var gdi = new BinaryWriter(new BufferedStream(File.OpenWrite(gdiPath)));

        // todo: get actual start/end
        var x1 = _zone.Location.X;
        var y1 = _zone.Location.Y;
        var x2 = _zone.Location.X;
        var y2 = _zone.Location.Y;

        gdi.Write(x1);
        gdi.Write(y1);
        gdi.Write(x2);
        gdi.Write(y2);

        gdi.Write(_nodes.Count);

        // todo: write the rest of the arrays
        using var nod = new BinaryWriter(new BufferedStream(File.OpenWrite(Path.ChangeExtension(gdiPath, "nod"))));

        foreach (var node in _nodes.Values)
        {
            nod.Write(node.X);
            nod.Write(node.Y);
            nod.Write(node.Z);

            foreach (var nidx in node.Neighbors)
            {
                // TODO: recalculate node index before this
                nod.Write(nidx);
            }

            foreach (var dist in node.Distances)
            {
                nod.Write(dist);
            }
        }
    }

    bool IsWalkable(Vector3 start, Vector3 end)
    {
        var heading = (end - start) with { Z = 0 };
        var headingSquared = Vector3.DistanceSquared(start, end);

        if (Math.Abs(headingSquared - 1) < 0.01 || headingSquared > 1)
            heading = heading / float.Sqrt(headingSquared) * 15;

        var current = start;
        var next = start;
        var currentCellZ = (int)start.Z;

        var step = 0;
        while (true)
        {
            while (true)
            {
                if (step++ > 1000) return false;
                if (current == end)
                {
                    var zDiff = float.Abs(end.Z - currentCellZ);
                    return float.Abs(zDiff) <= 15;
                }

                next = Vector3.DistanceSquared(current, end) >= 16 * 16
                    ? next + heading
                    : end;

                if (Utils.GetCellIndexFromPoint(current.ToVector2())
                 != Utils.GetCellIndexFromPoint(next.ToVector2()))
                    break;

                current = next;
            }

            var currentCell = Utils.GetCellIndexFromPoint(current.ToVector2());
            var nextCell = Utils.GetCellIndexFromPoint(next.ToVector2());

            var nextVolume = CanGoNeighbourhoodCell(currentCell, nextCell, currentCellZ);
            if (nextVolume == null) return false;

            currentCellZ = nextVolume.Value.Z;
            current = next;
        }
    }

    Volume? CanGoNeighbourhoodCell(CellIndex current, CellIndex next, int z)
    {
        if (int.Abs(current.GetX() - next.GetX()) > 1 ||
            int.Abs(current.GetY() - next.GetY()) > 1)
            return null;

        var allNextVolumes = GetIndexedVolumesAtCell(next);
        var nextVolume = GetCellVolumeAt(allNextVolumes, z + 15, false);

        if (nextVolume == null)
            return null;

        if (z + 50 >= nextVolume.Value.Volume.Z + nextVolume.Value.Volume.Height)
            return null;

        if (z > nextVolume.Value.Volume.Z + 50)
            return null;

        return nextVolume.Value.Volume;
    }

    List<IndexedVolume> GetIndexedVolumesAtCell(CellIndex indexedVolume)
    {
        var list = new List<IndexedVolume>();
        int searchIdx = 0;

        while (_indexer.IndexedVolumes.TryGetValue(indexedVolume with { VolumeIdx = searchIdx }, out var neigh))
        {
            list.Add(neigh);
            searchIdx++;
        }

        return list;
    }

    static IndexedVolume? GetCellVolumeAt(IEnumerable<IndexedVolume> cell, int z, bool alsoSearchAbove)
    {
        if (z == -16777215)
            return cell.FirstOrDefault();

        return alsoSearchAbove
            ? cell.MinBy(idx => int.Abs(idx.Volume.Z - z))
            : cell.LastOrDefault(idx => idx.Volume.Z <= z);
    }
}