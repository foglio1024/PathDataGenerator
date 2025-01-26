using System.Collections.ObjectModel;

namespace PathDataGenerator;

class Indexer
{
    public readonly ReadOnlyDictionary<CellIndex, IndexedVolume> CellIndexToIndexedVolume;
    public readonly ReadOnlyCollection<IndexedVolume> IndexedVolumes;
    public readonly ReadOnlyDictionary<CellIndex, int> CellIndexToVolumeIndex;

    public Indexer(Area area)
    {
        var tmpDict = new Dictionary<CellIndex, IndexedVolume>();
        for (int zx = 0; zx < area.Size.Width; zx++)
        {
            for (int zy = 0; zy < area.Size.Height; zy++)
            {
                var absx = area.Start.X + zx;
                var absy = area.Start.Y + zy;
                Console.WriteLine($"Searching zone @ ({absx}, {absy})");
                var found = area.Zones.Any(zn => zn.Location.X == absx && zn.Location.Y == absy);

                if (!found)
                {
                    Console.WriteLine("!! Zone not found !!");
                    continue;
                }

                try
                {
                    for (int sx = 0; sx < Generator.NUM_SQUARES; sx++)
                    {
                        for (int sy = 0; sy < Generator.NUM_SQUARES; sy++)
                        {
                            for (int cx = 0; cx < Generator.NUM_CELLS; cx++)
                            {
                                for (int cy = 0; cy < Generator.NUM_CELLS; cy++)
                                {
                                    var vols = area.Zones.FirstOrDefault(z => z.Location.X == absx && z.Location.Y == absy)//[zx + zy * area.Size.Width]
                                                   .Squares[sx, sy]
                                                   .Cells[cx, cy]
                                                   .Volumes;

                                    for (int vidx = 0; vidx < vols.Length; vidx++)
                                    {
                                        var cidx = new CellIndex(zx, zy, sx, sy, cx, cy, vidx);
                                        tmpDict[cidx] = new IndexedVolume(cidx, vols[vidx]);
                                    }
                                }
                            }
                        }
                    }

                }
                catch
                {
                }
            }
        }

        CellIndexToIndexedVolume = new ReadOnlyDictionary<CellIndex, IndexedVolume>(tmpDict);
        IndexedVolumes = CellIndexToIndexedVolume.Values.ToArray().AsReadOnly();

        CellIndexToVolumeIndex = IndexedVolumes
            .Select((cell, i) => (cell.Index, i))
            .ToDictionary()
            .AsReadOnly();

    }

    public List<IndexedVolume> GetIndexedVolumesAtCell(CellIndex indexedVolume)
    {
        var list = new List<IndexedVolume>();
        int searchIdx = 0;

        while (CellIndexToIndexedVolume.TryGetValue(indexedVolume with { VolumeIdx = searchIdx }, out var neigh))
        {
            list.Add(neigh);
            searchIdx++;
        }

        return list;
    }
}
