using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WFC : MonoBehaviour
{
    public string BitmapPath;
    public string GraphPath;
    public Texture2D BitmapTexture;
    public Dictionary<string, Node> Nodes;

    //TODO Sample List in Node or smth
    public int SampleSize;
    public int Width, Height;
    public bool PeriodicInput;
    public bool Periodic;
    public bool Symmetric;
    public bool Ground;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadJSON()
    {
        Nodes = JSONParser.ParseJSON(BitmapPath);
    }
}
