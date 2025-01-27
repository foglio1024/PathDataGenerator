using System.Numerics;

namespace PathDataGenerator;

public static class Extensions
{
    public static Vector3 ToVector3(this Vector2 v) => new Vector3(v.X, v.Y, 0);

    public static Vector2 ToVector2(this Vector3 v) => new Vector2(v.X, v.Y);

    public static byte[] ToByteArrayHex(this string hexStr)
    {
        int num = hexStr.Length / 2;
        byte[] array = new byte[num];
        using StringReader stringReader = new StringReader(hexStr);
        for (int i = 0; i < num; i++)
        {
            array[i] = Convert.ToByte(new string(new char[2]
            {
                (char)stringReader.Read(),
                (char)stringReader.Read()
            }), 16);
        }

        return array;
    }
}
