using B83.Image.BMP;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InputImage
{
    public static BMPImage LoadImage(string path)
    {
        BMPLoader loader = new BMPLoader();
        BMPImage img = loader.LoadBMP(path);
        return img;
    }

    public static Texture2D LoadPNG(string path)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(path))
        {
            fileData = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            tex.filterMode = FilterMode.Point;
        }
        return tex;
    }
}
