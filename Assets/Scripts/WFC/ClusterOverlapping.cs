using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterOverlapping : NewModel
{
    //Dictionary<string, List<byte[]>> patterns;      //Node or Clustername to Patterns
    Dictionary<string, List<int>> colors;
    //List<int> colors;                               //Eine Liste an Farben für alle Nodes

    //List<byte[]> patterns;                          //global/all patterns
    Dictionary<string, List<byte[]>> patterns;        //per node patterns

    public ClusterOverlapping(Dictionary<string, Node> nodes, Texture2D clusterMap, int N, int width, int height, bool periodicInput, bool periodic, int symmetry, bool ground, Heuristic heuristic)
        : base(width, height, N, periodic, heuristic)
    {
        //load all samples for each node
        //Dictionary<string, byte[]> samples = new();
        colors = new();
        patterns = new();
        weights = new();

        foreach(var node in nodes)
        {
            int[] nodeBitmap = node.Value.Sample.GetBitmap();
            int sx = node.Value.Sample.width;
            int sy = node.Value.Sample.height;

            List<int> nodeColors = new();


            //get all colors from all samples and prepare indexed sample array for each node
            byte[] nodeSample = new byte[nodeBitmap.Length];

            for(int i = 0; i < nodeSample.Length; i++)
            {
                int color = nodeBitmap[i];
                int k = 0;
                for(; k < nodeColors.Count; k++)
                {
                    if (nodeColors[k] == color) break;
                    if(k == nodeColors.Count) nodeColors.Add(color);
                    nodeSample[i] = (byte)k;
                }
            }

            //samples.Add(node.Key, nodeSample);


            List<byte[]> nodePatterns = new();
            Dictionary<long, int> patternIndices = new();
            List<double> weightList = new();

            int C = nodeColors.Count;
            int xmax = periodicInput ? sx : sx - N + 1;
            int ymax = periodicInput ? sy : sy - N + 1;

            for (int y = 0; y < ymax; y++)
            {
                for (int x = 0; x < xmax; x++)
                {
                    byte[][] ps = new byte[8][];

                    ps[0] = pattern((dx, dy) => nodeSample[(x + dx) % sx + (y + dy) % sy * sx], N);
                    ps[1] = reflect(ps[0], N);
                    ps[2] = rotate(ps[0], N);
                    ps[3] = reflect(ps[2], N);
                    ps[4] = rotate(ps[2], N);
                    ps[5] = reflect(ps[4], N);
                    ps[6] = rotate(ps[4], N);
                    ps[7] = reflect(ps[6], N);

                    for (int k = 0; k < symmetry; k++)
                    {

                        //Pattern wird gehashed anhand dessen Inhalt. Gibt es schon ein Pattern mit demselben Hashwert, so ist es ein duplikat und wird nicht zur Liste hinzugefügt.
                        byte[] p = ps[k];
                        long h = hash(p, C);
                        if (patternIndices.TryGetValue(h, out int index)) weightList[index] = weightList[index] + 1;    //Umso öfter ein Pattern vorkam, desto höher steigt die gewichtung (weight) von diesem
                        else
                        {
                            patternIndices.Add(h, weightList.Count);
                            weightList.Add(1.0);
                            nodePatterns.Add(p);
                        }
                    }
                }
            }

            weights.Add(node.Key, weightList.ToArray());     //Gewichtung, wie oft ein pattern vorkam
            T = weightList.Count;
            this.ground = ground;

            //get all patterns and save them for each node (needs all colors to calculate hashes)

            //create propagator with clusterMap in mind

            int[][][] nodePropagator = new int[4][][];
            for (int d = 0; d < 4; d++)
            {
                nodePropagator[d] = new int[T][];   //T ist die Anzahl an einmaligen Patterns
                for (int t = 0; t < T; t++)
                {
                    List<int> list = new();
                    for (int t2 = 0; t2 < T; t2++)
                    {
                        if (agrees(nodePatterns[t], nodePatterns[t2], dx[d], dy[d], N)) list.Add(t2);
                    }

                    nodePropagator[d][t] = new int[list.Count];
                    for (int c = 0; c < list.Count; c++)
                    {
                        nodePropagator[d][t][c] = list[c];
                    }
                }
            }
            propagator.Add(node.Key, nodePropagator);
        }
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
    static long hash(byte[] p, int C)
    {
        long result = 0, power = 1;
        for (int i = 0; i < p.Length; i++)
        {
            result += p[p.Length - 1 - i] * power;
            power *= C;
        }
        return result;
    }

    /// <summary>
    /// Funktion die überprüft, ob zwei Patterns an bestimmten Stellen übereinstimmen
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="dx"></param>
    /// <param name="dy"></param>
    /// <param name="N"></param>
    /// <returns></returns>
    static bool agrees(byte[] p1, byte[] p2, int dx, int dy, int N)
    {
        int xmin = dx < 0 ? 0 : dx,
            xmax = dx < 0 ? dx + N : N,
            ymin = dy < 0 ? 0 : dy,
            ymax = dy < 0 ? dy + N : N;

        for (int y = ymin; y < ymax; y++)
        {
            for (int x = xmin; x < xmax; x++)
            {
                if (p1[x + N * y] != p2[x - dx + N * (y - dy)]) return false;
            }
        }

        return true;
    }
}
