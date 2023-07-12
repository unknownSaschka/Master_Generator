using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extentions
{
    public static int[] GetBitmap(this Texture2D texture)
    {
        Color32[] imageData = texture.GetPixels32();

        //Converts Color32 Array to an int array with bgra32
        int[] img = new int[texture.width * texture.height];
        for (int i = 0; i < imageData.Length; i++)
        {
            byte[] bytes = new byte[4];
            bytes[0] = imageData[i].b;
            bytes[1] = imageData[i].g;
            bytes[2] = imageData[i].r;
            bytes[3] = imageData[i].a;
            img[i] = BitConverter.ToInt32(bytes, 0);
        }

        return img;
    }
}
