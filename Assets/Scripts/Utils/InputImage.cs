using B83.Image.BMP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputImage
{
    public static Texture2D LoadImage(string path)
    {
        BMPLoader loader = new BMPLoader();
        BMPImage img = loader.LoadBMP(path);
        return img.ToTexture2D();
    }
}
