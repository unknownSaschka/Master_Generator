using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;

public class JSONParser
{
    
    public static Dictionary<string, Node> ParseJSON(string path)
    {
        Dictionary<string, Node> nodes = new Dictionary<string, Node>();

        JObject json = JObject.Parse(File.ReadAllText(path));
        var token = json.GetValue("nodes");

        foreach(var content in token.Values())
        {
            Node node = new Node();

            JProperty parentProp = (JProperty)content.Parent;
            //Debug.Log(parentProp.Name);   //Property name

            string samplePath = content.Value<string>("sample");
            if (samplePath != null)
            {
                samplePath = Directory.GetParent(path) + "/" + samplePath;
                node.Sample = LoadImg(samplePath);
                node.Children = null;

                int sym = content.Value<int>("symmetry");
                if (sym == 0) sym = 2;
                node.Symmetry = sym;

                bool periodic = content.Value<bool>("periodic");
                node.Periodic = periodic;
                
            }
            else
            {
                node.Sample = null;
            }

            //load adjacenties
            /*
            var adjacentiesList = content.Value<JObject>("adjacenties");

            if (adjacentiesList != null)
            {
                Adjacenties adjacenties = new Adjacenties();
                adjacenties.Up = adjacentiesList.Value<JArray>("U").ToObject<List<string>>();
                adjacenties.Down = adjacentiesList.Value<JArray>("D").ToObject<List<string>>();
                adjacenties.Left = adjacentiesList.Value<JArray>("L").ToObject<List<string>>();
                adjacenties.Right = adjacentiesList.Value<JArray>("R").ToObject<List<string>>();
                node.Adjacenties = adjacenties;
            }
            */

            //load color
            int[] colorVal = content.Value<JArray>("color")?.ToObject<int[]>();
            if(colorVal != null)
            {
                node.NodeColor = new Color32((byte)colorVal[0], (byte)colorVal[1], (byte)colorVal[2], 255);
                node.PrototypePlaceable = true;
            }
            else
            {
                node.PrototypePlaceable = false;
            }

            //load children list
            string[] children = content.Value<JArray>("children")?.ToObject<string[]>();
            if(children != null) node.Children = children.ToList();
            else node.Children = null;

            nodes.Add(parentProp.Name, node);
        }

        CalculateDepths(nodes, "root");

        return nodes;
    }

    private static Texture2D LoadImg(string path)
    {
        byte[] img = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(img);
        return tex;
    }

    private static void CalculateDepths(Dictionary<string, Node> nodes, string rootName)
    {
        Node rootNode = nodes[rootName];

        foreach(var targetNode in nodes)
        {
            Depth(nodes, targetNode, new KeyValuePair<string, Node>(rootName, rootNode), rootName, 0);
        }
    }

    private static bool Depth(Dictionary<string, Node> nodes, KeyValuePair<string, Node> targetNode, KeyValuePair<string, Node> currentNode, string rootName, int depth)
    {
        if (targetNode.Value.Equals(currentNode.Value))
        {
            currentNode.Value.Depth = depth;
            return true;
        }

        var next = currentNode.Value.Children;
        if (next == null) return false;

        foreach(var child in next)
        {
            if(Depth(nodes, targetNode, new KeyValuePair<string, Node>(child, nodes[child]), rootName, depth + 1))
            {
                return true;
            }
        }

        return false;

    }

}
