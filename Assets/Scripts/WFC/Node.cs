using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Node
{
    public Color32 NodeColor;
    public List<string> Children;
    public Adjacenties Adjacenties;
    public Texture2D Sample;
    //private Dictionary<int, List<byte[]>> patterns;    //saves all patterns in all sizes
    public OverlappingModel OverlappingModel;           //TODO Hier pro Node ein Overlapping Model laden


    //LCA Prpblem solving
    public bool Visited;
    public int Depth;
    public string Parent;
}

public class Adjacenties
{
    public List<string> Up = new List<string>();
    public List<string> Down = new List<string>();
    public List<string> Left = new List<string>();
    public List<string> Right = new List<string>();
}
