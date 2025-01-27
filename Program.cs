using System.Diagnostics;
using System.Numerics;
using PathDataGenerator;

var sw = Stopwatch.StartNew();

Generate();
sw.Stop();
Console.WriteLine($"Took {sw.Elapsed}");

// ------------------------------------------

void Generate()
{
    //Utils.NUM_CELLS = 4;
    //Utils.TOPO_PATH = "E:/TERA_DEV/out/atw_half/"; // halfres topo

    var generator = new Generator("ATW_P");

    Console.WriteLine("Created generator");

    generator.GenerateNodes();

    Console.WriteLine("Generated nodes");

    generator.WriteNavdata("E:/TERA_DEV/out");
}