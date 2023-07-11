using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterOverlapping : Model
{
    Dictionary<string, List<byte[]>> patterns;      //Node or Clustername to Patterns
    List<int> colors;

    public ClusterOverlapping(Dictionary<string, Node> nodes, Texture2D clusterMap, int N, int width, int height, bool periodicInput, bool periodic, int symmetry, bool ground, Heuristic heuristic)
        : base(width, height, N, periodic, heuristic)
    {
        Dictionary<string, byte[]> samples = new();

        //load all samples for each node

        //get all colors from all samples

        //get all patterns and save them for each node (needs all colors to calculate hashes)

        //calculate proper weights from all patterns

        //create propagator with clusterMap in mind

    }


    public override void Save(string filename)
    {
        throw new System.NotImplementedException();
    }

    static byte[] pattern(Func<int, int, byte> f, int N)
    {
        byte[] result = new byte[N * N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) result[x + y * N] = f(x, y);
        return result;
    }
    static byte[] rotate(byte[] p, int N) => pattern((x, y) => p[N - 1 - y + x * N], N);
    static byte[] reflect(byte[] p, int N) => pattern((x, y) => p[N - 1 - x + y * N], N);
}
