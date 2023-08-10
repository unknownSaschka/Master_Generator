// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using B83.Image.BMP;
using UnityEngine;

public static class Helper
{
    public enum Heuristic { Entropy, MRV, Scanline };

    /// <summary>
    /// Gibt anhand der Gewichtungen der einzelnen Elemente des Arrays eine zufällige Position
    /// </summary>
    /// <param name="weights"></param>
    /// <param name="r"></param>
    /// <returns></returns>
    public static int Random(this double[] weights, double r)
    {
        double sum = 0;
        for (int i = 0; i < weights.Length; i++) sum += weights[i];
        double threshold = r * sum;

        double partialSum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            partialSum += weights[i];
            if (partialSum >= threshold) return i;
        }
        return 0;
    }

    //NEW HELPER FOR Random on weights Dictionary
    public static int Random(this Dictionary<int, double> weights, double r)
    {
        double sum = 0;
        foreach(var weight in weights) sum += weight.Value;
        double threshold = r * sum;

        double partialSum = 0;
        foreach(var weight in weights)
        {
            partialSum += weight.Value;
            if(partialSum >= threshold) return weight.Key;
        }
        return 0;
    }

    public static long ToPower(this int a, int n)
    {
        long product = 1;
        for (int i = 0; i < n; i++) product *= a;
        return product;
    }

    public static T Get<T>(this XElement xelem, string attribute, T defaultT = default)
    {
        XAttribute a = xelem.Attribute(attribute);
        return a == null ? defaultT : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(a.Value);
    }

    public static IEnumerable<XElement> Elements(this XElement xelement, params string[] names) => xelement.Elements().Where(e => names.Any(n => n == e.Name));
}

static class BitmapHelper
{
    public static (int[], int, int) LoadBitmap(string filename)
    {
        /*
        using var image = Image.Load<Bgra32>(filename);
        int width = image.Width, height = image.Height;
        int[] result = new int[width * height];
        image.CopyPixelDataTo(MemoryMarshal.Cast<int, Bgra32>(result));
        return (result, width, height);
        */
        BMPLoader loader = new();
        var image = loader.LoadBMP(filename);
        return (image.ToIntArray(), image.info.width, image.info.height);
    }

    unsafe public static void SaveBitmap(int[] data, int width, int height, string filename)
    {
        /*
        fixed (int* pData = data)
        {
            using var image = Image.WrapMemory<Bgra32>(pData, width, height);
            image.SaveAsPng(filename);
        }
        */
        Texture2D texture = new Texture2D(width, height, TextureFormat.BGRA32, false);
        //TODO: Gescheites Image Saving machen
    }
}
