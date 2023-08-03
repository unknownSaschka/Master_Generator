using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrototypeParser 
{
    Dictionary<Color32, string> colorMapping;
    string rootName;
    Color32 rootColor;

    public Texture2D ProcessPrototype(Texture2D prototype, Graph graph, string rootName)
    {
        colorMapping = new();
        this.rootName = rootName;

        Color32[] colors = prototype.GetPixels32(); ;

        foreach(var node in graph.Nodes)
        {
            colorMapping.Add(node.Value.NodeColor, node.Key);
            if (node.Key.Equals(rootName))
            {
                rootColor = node.Value.NodeColor;
            }
        }

        for(int y = 0; y < prototype.height; y++)
        {
            for(int x = 0; x < prototype.width; x++)
            {
                Check(x, y, x + 1, y, prototype, colors);
                Check(x, y, x - 1, y, prototype, colors);
                Check(x, y, x, y + 1, prototype, colors);
                Check(x, y, x, y - 1, prototype, colors);
            }
        }

        //prototype.SetPixels32(colors);
        //return prototype;

        Texture2D newPrototype = new Texture2D(prototype.width, prototype.height);
        newPrototype.SetPixels32(colors);
        newPrototype.filterMode = FilterMode.Point;
        newPrototype.Apply();
        return newPrototype;
    }

    private bool Check(int x, int y, int x1, int y1, Texture2D prototype, Color32[] colors)
    {
        if (x < 0 || x >= prototype.width) return false;
        if (x1 < 0 || x1 >= prototype.width) return false;
        if (y < 0 || y >= prototype.height) return false;
        if (y1 < 0 || y1 >= prototype.height) return false;

        string pixel1 = colorMapping[colors[x + y * prototype.width]];
        string pixel2 = colorMapping[colors[x1 + y1 * prototype.width]];

        if(pixel1.Equals(rootName) || pixel2.Equals(rootName)) return false;

        if (pixel1.Equals(pixel2)) return false;

        colors[x + y * prototype.width] = rootColor;
        colors[x1 + y1 * prototype.width] = rootColor;
        return true;
    }
}
