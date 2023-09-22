using System.Collections.ObjectModel;
using System.Numerics;

namespace PathDataGenerator;

class Indexer
{
    public readonly ReadOnlyDictionary<CellIndex, IndexedVolume> IndexedVolumes;
    public readonly ReadOnlyCollection<IndexedVolume> VolumesArray;
    public readonly ReadOnlyDictionary<CellIndex, int> VolumeIndices;

    public Indexer(Zone zone)
    {
        var tmpDict = new Dictionary<CellIndex, IndexedVolume>();

        for (int sx = 0; sx < Generator.NUM_SQUARES; sx++)
        {
            for (int sy = 0; sy < Generator.NUM_SQUARES; sy++)
            {
                for (int cx = 0; cx < Generator.NUM_CELLS; cx++)
                {
                    for (int cy = 0; cy < Generator.NUM_CELLS; cy++)
                    {
                        var vols = zone.Squares[sx, sy].Cells[cx, cy].Volumes;

                        for (int vidx = 0; vidx < vols.Length; vidx++)
                        {
                            var cidx = new CellIndex(sx, sy, cx, cy, vidx);
                            tmpDict[cidx] = new IndexedVolume(cidx, vols[vidx]);
                        }

                    }
                }
            }
        }
        IndexedVolumes = new ReadOnlyDictionary<CellIndex, IndexedVolume>(tmpDict);
        VolumesArray = IndexedVolumes.Values.ToArray().AsReadOnly();

        var tmpDict2 = new Dictionary<CellIndex, int>();

        for (int i = 0; i < VolumesArray.Count; i++)
        {
            var cell = VolumesArray[i];
            tmpDict2[cell.Index] = i;
        }

        VolumeIndices = new ReadOnlyDictionary<CellIndex, int>(tmpDict2);
    }


}
