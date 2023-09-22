using System.Numerics;

namespace PathDataGenerator;

public static class Extensions
{
    public static Vector3 ToVector3(this Vector2 v) => new Vector3(v.X, v.Y, 0);

    public static Vector2 ToVector2(this Vector3 v) => new Vector2(v.X, v.Y);
}
