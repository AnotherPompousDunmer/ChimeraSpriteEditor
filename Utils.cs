using Godot;
using System;
using System.Collections.Generic;

public class Utils
{
    
    internal static int MaxI(int x, int y)
	{
        return (x > y) ? x : y;
	}
    public static int ClampI(int min, int val, int max)
    {
        return min > val ? min : max < val ? max : val;
    }

    internal static int AbsI(int i)
	{
        return (i < 0) ? -i : i;
	}

	internal static Vector2 ClampV(Vector2 min, Vector2 val, Vector2 max)
	{
        val.x = Mathf.Clamp(val.x, min.x, max.x);
        val.y = Mathf.Clamp(val.y, min.y, max.y);
        return val;
    }

    internal static Vector2 ClampV(float min, Vector2 val, float max)
    {
        val.x = Mathf.Clamp(val.x, min, max);
        val.y = Mathf.Clamp(val.y, min, max);
        return val;
    }

    internal static Vector2 MinV(Vector2 x, Vector2 y)
	{
        return new Vector2(Mathf.Min(x.x, y.x), Mathf.Min(x.y, y.y));
	}

    internal static Vector2 AbsV(Vector2 v)
	{
        v.x = Mathf.Abs(v.x);
        v.y = Mathf.Abs(v.y);
        return v;
    }

    internal static Vector2 VectorCompare(bool minMax, params Vector2[] ins)
	{
        Vector2 result = ins[0];

        for (int i = 1; i < ins.Length; i++)
		{
            result = minMax ? MaxV(result, ins[i]) : MinV(result, ins[i]);
		}

        return result;
	}

    internal static Vector2 MaxV(Vector2 x, Vector2 y)
    {
        return new Vector2(Mathf.Max(x.x, y.x), Mathf.Max(x.y, y.y));
    }

    internal static Vector2 RoundV(Vector2 val)
	{
        return new Vector2((int)(val.x + 0.5f), (int)(val.y + 0.5f));
	}

    internal static Vector2 FloorV(Vector2 val)
    {
        return new Vector2((int)val.x, (int)val.y);
    }

    internal static Vector2 CeilV(Vector2 val)
    {
        return new Vector2((int)(val.x + 1f), (int)(val.y + 1f));
    }

    internal static bool VecHasPoint(Vector2 rect, Vector2 point)
	{
        return (point.x >= 0 && point.y >= 0 && point.x < rect.x && point.y < rect.y);
	}

    internal static Vector2[] GetRectCorners(Rect2 r)
	{
        return new Vector2[] { r.Position, new Vector2(r.End.x, r.Position.y), r.End, new Vector2(r.Position.x, r.End.y) };
    }

    internal static Image StringToImage(string str, Vector2 size)
	{
        Image img = new Image();
        img.Create((int)size.x, (int)size.y, false, Image.Format.Rgba8);
        img.Lock();

        char[] imgInf = str.ToCharArray();

        //Old algorithm
        /*for (int i = 0; i < imgInf.Length; i += 4)
        {
            Color pixel = new Color();
            pixel.r8 = CharToInt(imgInf[i]);
            pixel.g8 = CharToInt(imgInf[i + 1]);
            pixel.b8 = CharToInt(imgInf[i + 2]);
            pixel.a8 = CharToInt(imgInf[i + 3]);

            img.SetPixel((i / 4) / (int)size.x, (i / 4) % (int)size.x, pixel);
        }*/

        for (int y = 0; y < size.y; y++) for (int x = 0; x < size.x; x++)
			{
                Color pixel = new Color();
                int i = ((x*(int)size.y) + y)*4;
                pixel.r8 = CharToInt(imgInf[i]);
                pixel.g8 = CharToInt(imgInf[i + 1]);
                pixel.b8 = CharToInt(imgInf[i + 2]);
                pixel.a8 = CharToInt(imgInf[i + 3]);

                img.SetPixel(x, y, pixel);
            }

        img.Unlock();
        return img;
    }

    internal static string ImageToString(Image img)
    {
        img.Lock();
        Vector2 size = img.GetSize();
        string output = "";
        for (int i = 0; i < (int)(size.x * size.y); i++)
        {
            Color pixel = img.GetPixel(i / (int)size.y, i % (int)size.y);
            string colorStr = "" + IntToChar(pixel.r8) + IntToChar(pixel.g8) + IntToChar(pixel.b8) + IntToChar(pixel.a8);
            output += colorStr;
		}
        img.Unlock();
        return output;
    }
    private static char IntToChar(int i)
	{
        return (char)(ushort)(i + 256);
	}
    private static int CharToInt(char c)
    {
        return (int)((ushort)c) - 256;
    }

    //From https://stackoverflow.com/questions/28018147/emgucv-get-coordinates-of-pixels-in-a-line-between-two-points
    public static HashSet<Vector2> GetBresenhamLine(Vector2 p0, Vector2 p1)
    {
        int x0 = Mathf.RoundToInt(p0.x);
        int y0 = Mathf.RoundToInt(p0.y);
        int x1 = Mathf.RoundToInt(p1.x);
        int y1 = Mathf.RoundToInt(p1.y);
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);

        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        int err = dx - dy;

        var points = new HashSet<Vector2>();

        while (true)
        {
            points.Add(new Vector2(x0, y0));
            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err = err - dy;
                x0 = x0 + sx;
            }
            if (e2 < dx)
            {
                err = err + dx;
                y0 = y0 + sy;
            }
        }

        return points;
    }

	internal static HashSet<Vector2> GetCurvedLine(List<Vector2> points)
	{
        int r = 0;
        while (r < points.Count-1)
		{
            if (points[r] == points[r+1])
			{
                points.RemoveAt(r + 1);
			}
            else
			{
                r++;
			}
		}

        //Check the list is big enough
        if (points.Count == 2)
		{
            return GetBresenhamLine(points[0], points[1]);
		}
        else if (points.Count < 2)
		{
            return new HashSet<Vector2>(points);
        }

        HashSet<Vector2> pixLine = new HashSet<Vector2>();

        return pixLine;
	}

    public static int b2i(bool b) { return b ? 1 : 0; }

    internal static void SimpleImageDoubleMult (Image baseImg, Image mult)
	{
        Vector2 size = baseImg.GetSize();

        baseImg.Lock();
        mult.Lock();

        for (int x = 0; x < size.x; x++) for (int y = 0; y < size.y; y++)
			{
                Color c = baseImg.GetPixel(x, y);
                Color m = mult.GetPixel(x, y) * 2;
                c.r *= m.r;
                c.g *= m.g;
                c.b *= m.b;
                baseImg.SetPixel(x, y, c);
			}

        baseImg.Unlock();
        mult.Unlock();
    }

	internal static Image BlendImages(List<Image> images)
	{
        Image blended = new Image();
        blended.Create(images[0].GetHeight(), images[0].GetWidth(), false, Image.Format.Rgba8);
        Rect2 srcRect = new Rect2(Vector2.Zero, images[0].GetSize());
		foreach (Image i in images)
		{
            blended.BlendRect(i, srcRect, Vector2.Zero);
		}
        return blended;
	}

	internal static T[] GetSortedDictionaryKeys<T, U>(Dictionary<T, U> dict)
	{
        T[] output = new T[dict.Count];
        dict.Keys.CopyTo(output, 0);
        Array.Sort(output);
        return output;
    }

    internal static int ArraySafeMod(int a, int b)
	{
        return Mathf.Sign(a) * (a % b);
	}

}
