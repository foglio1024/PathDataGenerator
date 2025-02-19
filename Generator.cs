﻿using System.Collections.Concurrent;
using System.Numerics;

namespace PathDataGenerator;

internal class Generator
{

    private readonly Indexer _indexer;
    private ConcurrentDictionary<int, Node>? _nodes;
    private readonly string _areaName;

    public static Area CurrentArea { get; private set; }

    public Generator(string areaName)
    {
        _areaName = areaName;

        CurrentArea = Area.Create(areaName);

        _indexer = new Indexer(CurrentArea);
    }


    public ConcurrentDictionary<int, Node> GenerateNodes()
    {
        var total = _indexer.CellIndexToIndexedVolume.Count;

        Console.WriteLine($"Indexed cells: {total}");

        var done = 0;

        var nodes = new ConcurrentDictionary<int, Node>();
        Parallel.For(0, total, new ParallelOptions { MaxDegreeOfParallelism = 16 }, idx =>
        {
            var v = _indexer.IndexedVolumes[idx];


            var step = 1;

            var neighbourCells = new[]
            {
                v.Index.AddX(-step),
                v.Index.AddX(-step).AddY(step),
                v.Index.AddY(step),
                v.Index.AddY(step).AddX(step),
                v.Index.AddX(step),
                v.Index.AddY(-step).AddX(step),
                v.Index.AddY(-step),
                v.Index.AddY(-step).AddX(-step),
            };

            var neighbourIndexedVolumes =
                neighbourCells.Select(cell => _indexer.CellIndexToIndexedVolume.TryGetValue(cell, out var neigh)
                    ? neigh
                    : default).ToArray();

            var volume = v.Volume;
            var neighbors = new int[8];
            var distances = new int[8];
            var currCellPos = CurrentArea.GetCellPos(v.Index).ToVector3() with { Z = volume.Z };

            for (int i = 0; i < 8; i++)
            {
                var niv = neighbourIndexedVolumes[i];
                if (niv == default)
                {
                    neighbors[i] = -1;
                    distances[i] = int.MaxValue;
                    continue;
                }

                var neighs = _indexer.GetIndexedVolumesAtCell(niv.Index);

                var neighVolume = GetCellVolumeAt(neighs.ToArray(), volume.Z + 15, false);

                var neighCellPos = CurrentArea.GetCellPos(niv.Index).ToVector3();

                if (neighVolume == default
                || !IsWalkable(currCellPos, neighCellPos with { Z = neighVolume.Volume.Z })
                // || (neighVolume.Index.CX % 2 == 1 || neighVolume.Index.CY % 2 == 1)
                )
                {
                    neighbors[i] = -1;
                    distances[i] = int.MaxValue;
                    continue;
                }

                neighCellPos = neighCellPos with { Z = neighVolume.Volume.Z };

                var dist = Vector3.Distance(neighCellPos, currCellPos);

                neighbors[i] = _indexer.CellIndexToNodeIndex[neighVolume.Index];
                distances[i] = Convert.ToInt32(dist);
            }

            var node = new Node(currCellPos.X, currCellPos.Y, currCellPos.Z, neighbors, distances);
            nodes[idx] = node;

            Interlocked.Increment(ref done);
            if (done % 1000 == 0) Console.Write($"\rGenerating... {done / (float)total:P1}");
        }
        );

        _nodes = nodes;
        return nodes;
    }

    public void WriteNavdata(string outputFolder)
    {
        var gdiPath = Path.Combine(outputFolder, $"pathdata_{_areaName}.gdi");

        Console.WriteLine($"\nSaving navdata to {gdiPath}");

        using var gdi = new BinaryWriter(new BufferedStream(File.Create(gdiPath)));

        gdi.Write((int)CurrentArea.Start.X);
        gdi.Write((int)CurrentArea.Start.Y);
        gdi.Write((int)CurrentArea.End.X);
        gdi.Write((int)CurrentArea.End.Y);

        if (_nodes == null) throw new InvalidOperationException("Nodes must be generated before calling WriteNavdata");

        var nodesInSquareList = new List<int>();

        // todo: write the rest of the arrays

        var nodesIndices = new List<int>();

        for (int zx = 0; zx < CurrentArea.Size.Width; zx++)
        {
            for (int zy = 0; zy < CurrentArea.Size.Height; zy++)
            {
                for (int sx = 0; sx < Utils.NUM_SQUARES; sx++)
                {
                    for (int sy = 0; sy < Utils.NUM_SQUARES; sy++)
                    {
                        var nodesInSquare = 0;
                        for (int cx = 0; cx < Utils.NUM_CELLS; cx++)
                        {
                            for (int cy = 0; cy < Utils.NUM_CELLS; cy++)
                            {
                                //if (cx % 2 == 1 || cy % 2 == 1) continue;
                                var cellIdx = new CellIndex(zx, zy, sx, sy, cx, cy, -1);
                                var vols = _indexer.GetIndexedVolumesAtCell(cellIdx);

                                foreach (var vol in vols)
                                {
                                    var nodeIdx = _indexer.CellIndexToNodeIndex[vol.Index];

                                    if (!_nodes.TryGetValue(nodeIdx, out var node))
                                        continue;

                                    nodesIndices.Add(nodeIdx);
                                    nodesInSquare++;
                                }
                            }
                        }
                        //gdi.Write(nodesInSquare);
                        nodesInSquareList.Add(nodesInSquare);
                        Console.Write($"\rWritten square({sx}, {sy})");
                    }
                }
            }
        }

        gdi.Write(_nodes.Count);
        //gdi.Write(nodesIndices.Count);
        foreach (var i in nodesInSquareList)
        {
            gdi.Write(i);
        }

        Console.WriteLine();
        for (int i = 0; i < nodesIndices.Count; i++)
        {
            int idx = nodesIndices[i];
            gdi.Write(idx);
            if (i % 1000 == 0 || i == nodesIndices.Count - 1) Console.Write($"\rWritten idx {i / (float)nodesIndices.Count:P1}");
        }
        Console.WriteLine();

        using var nod = new BinaryWriter(new BufferedStream(File.Create(Path.ChangeExtension(gdiPath, "nod"))));
        var offset = (CurrentArea.Start - CurrentArea.Origin);
        //foreach (var node in _nodes.Values)
        foreach (var node in nodesIndices.Select(i => _nodes[i]))
        {
            nod.Write(node.X + offset.X * Zone.UNIT_SIZE); // todo: move this translation before
            nod.Write(node.Y + offset.Y * Zone.UNIT_SIZE);
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

    private bool IsWalkable(Vector3 start, Vector3 end)
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

                next = Vector3.DistanceSquared(current, end) >= (16*16)
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

    private Volume? CanGoNeighbourhoodCell(CellIndex current, CellIndex next, int z)
    {
        if (int.Abs(current.GetX() - next.GetX()) > 1 ||
            int.Abs(current.GetY() - next.GetY()) > 1)
            return null;

        var allNextVolumes = _indexer.GetIndexedVolumesAtCell(next);
        var nextVolume = GetCellVolumeAt(allNextVolumes, z + 15, false);

        if (nextVolume == default)
            return null;

        if (z + 50 >= nextVolume.Volume.Z + nextVolume.Volume.Height)
            return null;

        if (z > nextVolume.Volume.Z + 50)
            return null;

        return nextVolume.Volume;
    }

    private static IndexedVolume GetCellVolumeAt(IEnumerable<IndexedVolume> cell, int z, bool alsoSearchAbove)
    {
        if (z == -16777215)
            return cell.FirstOrDefault();

        return alsoSearchAbove
            ? cell.MinBy(idx => int.Abs(idx.Volume.Z - z))
            : cell.LastOrDefault(idx => idx.Volume.Z <= z);
    }

}