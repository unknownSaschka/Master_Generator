using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        Color32[] colors = prototype.GetPixels32();

        foreach(var node in graph.Nodes)
        {
            if(node.Value.PrototypePlaceable) colorMapping.Add(node.Value.NodeColor, node.Key);
            if (node.Key.Equals(rootName))
            {
                rootColor = node.Value.NodeColor;
            }
        }

        for(int y = 0; y < prototype.height; y++)
        {
            for(int x = 0; x < prototype.width; x++)
            {
                Check(x, y, x + 1, y, prototype, colors, graph);
                Check(x, y, x - 1, y, prototype, colors, graph);
                Check(x, y, x, y + 1, prototype, colors, graph);
                Check(x, y, x, y - 1, prototype, colors, graph);
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

    private bool Check(int x, int y, int x1, int y1, Texture2D prototype, Color32[] colors, Graph graph)
    {
        if (x < 0 || x >= prototype.width) return false;
        if (x1 < 0 || x1 >= prototype.width) return false;
        if (y < 0 || y >= prototype.height) return false;
        if (y1 < 0 || y1 >= prototype.height) return false;

        string pixel1 = colorMapping[colors[x + y * prototype.width]];
        string pixel2 = colorMapping[colors[x1 + y1 * prototype.width]];

        //if(pixel1.Equals(rootName) || pixel2.Equals(rootName)) return false;
        var n = from s in graph.Nodes where (s.Value.Sample == null) select s.Key;
        if(n.Contains(pixel1) || n.Contains(pixel2))
        {
            return false;
        }

        if (pixel1.Equals(pixel2)) return false;

        colors[x + y * prototype.width] = rootColor;
        //colors[x1 + y1 * prototype.width] = rootColor;
        return true;
    }

    public Texture2D ExpandTexture(Texture2D oldTexture, int N)
    {
        int pixel = N - 1;
        Texture2D newTexture = new Texture2D(oldTexture.width + pixel, oldTexture.height + pixel);
        Color32[] colors = oldTexture.GetPixels32();
        Color32[] newColors = new Color32[newTexture.width * newTexture.height];

        for (int y = 0; y < newTexture.height; y++){
            for (int x = 0; x < newTexture.width; x++)
            {
                //int dx = x < newTexture.width - N + 1 ? 0 : N - 1;
                //int dy = y > newTexture.height - N + 1 ? 0 : N - 1;
                int dx = x;
                int dy1;
                int dy2;

                dy1 = N - 1;
                dy2 = y - dy1;

                if(dy2 <= 0)
                {
                    dy2 = 0;
                }
                
                if(x >= oldTexture.width)
                {
                    dx = oldTexture.width - 1;
                }

                //newTexture.SetPixel(x, y, colors[x - dx + (y - dy) * oldTexture.width]);
                newColors[x + y * newTexture.width] = colors[dx + (dy2) * oldTexture.width];
            }
        }

        newTexture.SetPixels32(newColors);
        newTexture.filterMode = FilterMode.Point;
        newTexture.Apply();
        return newTexture;
    }

    public static Texture2D CutTexture(Texture2D oldTexture, int N)
    {
        int pixel = N - 1;
        Texture2D newTexture = new Texture2D(oldTexture.width - pixel, oldTexture.height - pixel);
        Color32[] oldColors = oldTexture.GetPixels32();
        Color32[] newColors = new Color32[newTexture.width * newTexture.height];

        for (int y = 0; y < oldTexture.height; y++)
        {
            for (int x = 0; x < oldTexture.width; x++) 
            {
                int dx = x;
                int dy = y;

                if (dx >= newTexture.width) continue;
                if (y < pixel) continue;

                if(dy > newTexture.height) dy = newTexture.height + 1;

                newColors[x + (y - pixel) * newTexture.width] = oldColors[x + (dy) * oldTexture.width];

            }
        }

        newTexture.SetPixels32(newColors);
        newTexture.filterMode = FilterMode.Point;
        newTexture.Apply();
        return newTexture;
    }
}
