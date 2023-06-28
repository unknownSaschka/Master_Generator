using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Node
{
    public Color NodeColor;
    public List<string> Children;
    public Adjacenties Adjacenties;
    public Texture2D Sample;
    private List<byte[]> patterns;
}

public class Adjacenties
{
    public List<string> Up = new List<string>();
    public List<string> Down = new List<string>();
    public List<string> Left = new List<string>();
    public List<string> Right = new List<string>();
}
