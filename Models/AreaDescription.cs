using System.Collections.ObjectModel;
using System.Numerics;

namespace PathDataGenerator.Models;

internal readonly record struct AreaDescription(Vector2 Origin, ReadOnlyCollection<Vector2> ZoneLocations)
{

}