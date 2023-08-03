using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph
{
    public Dictionary<string, Node> Nodes;

    public Graph(Dictionary<string, Node> nodes)
    {
        Nodes = nodes;

        foreach(var node in Nodes)
        {
            node.Value.Visited = false;
        }
    }

    public KeyValuePair<string, Node>? LCA(string sNode1, string sNode2)
    {
        Node node1 = Nodes[sNode1];
        Node node2 = Nodes[sNode2];

        //Falls ein Node der Parent der anderen Node ist, so ist der Parent der Rückgabewert
        if (node1.Children.Contains(sNode2)) return new KeyValuePair<string, Node> (sNode1, Nodes[sNode1]);
        if (node2.Children.Contains(sNode1)) return new KeyValuePair<string, Node> (sNode2, Nodes[sNode2]);

        return null;
    }

    private void DFS(string node)
    {
        Nodes[node].Visited = true;

        foreach(string child in Nodes[node].Children)
        {
            if (Nodes[child].Visited == false)
            {
                Nodes[child].Depth = Nodes[node].Depth + 1;
                Nodes[child].Parent = node;
                DFS(child);
            }
        }
    }
}
