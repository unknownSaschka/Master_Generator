using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterOverlapping : Model
{
    //Dictionary<string, List<byte[]>> patterns;      //Node or Clustername to Patterns
    //Dictionary<string, List<int>> colors;
    List<int> colors;                               //Eine Liste an Farben für alle Nodes
    List<byte[]> patterns;

    public ClusterOverlapping(Dictionary<string, Node> nodes, Texture2D clusterMap, int N, int width, int height, bool periodicInput, bool periodic, int symmetry, bool ground, Heuristic heuristic)
        : base(width, height, N, periodic, heuristic)
    {
        //load all samples for each node
        Dictionary<string, byte[]> samples = new();
        colors = new();

        foreach(var node in nodes)
        {
            int[] nodeBitmap = node.Value.Sample.GetBitmap();


            //get all colors from all samples and prepare indexed sample array for each node
            byte[] nodeSample = new byte[nodeBitmap.Length];

            for(int i = 0; i < nodeSample.Length; i++)
            {
                int color = nodeBitmap[i];
                int k = 0;
                for(; k < colors.Count; k++)
                {
                    if (colors[k] == color) break;
                    if(k == colors.Count) colors.Add(color);
                    nodeSample[i] = (byte)k;
                }
            }

            samples.Add(node.Key, nodeSample);
        }



        //get all patterns and save them for each node (needs all colors to calculate hashes)
        patterns = new();
        Dictionary<long, int> patternIndices = new();
        List<double> weightList = new();

        int C = colors.Count;

        foreach(var node in nodes)
        {
            int sx = node.Value.Sample.width;
            int sy = node.Value.Sample.height;

            int xmax = periodicInput ? sx : sx - N + 1;
            int ymax = periodicInput ? sy : sy - N + 1;

            var sample = samples[node.Key];

            for (int y = 0; y < ymax; y++)
            {
                for (int x = 0; x < xmax; x++)
                {
                    byte[][] ps = new byte[8][];

                    ps[0] = pattern((dx, dy) => sample[(x + dx) % sx + (y + dy) % sy * sx], N);
                    ps[1] = reflect(ps[0], N);
                    ps[2] = rotate(ps[0], N);
                    ps[3] = reflect(ps[2], N);
                    ps[4] = rotate(ps[2], N);
                    ps[5] = reflect(ps[4], N);
                    ps[6] = rotate(ps[4], N);
                    ps[7] = reflect(ps[6], N);

                    for (int k = 0; k < symmetry; k++)
                    {
                        byte[] p = ps[k];
                        long h = hash(p, C);
                        if (patternIndices.TryGetValue(h, out int index)) weightList[index] = weightList[index] + 1;
                        else
                        {
                            patternIndices.Add(h, weightList.Count);
                            weightList.Add(1.0);        //WICHTIG: Evtl gewichtungen nochmals anders behalndeln, da nun Gewichtungen GLOBAL betrachtet werden, nicht mehr pro Bereich/Sample
                            patterns.Add(p);
                        }
                    }
                }
            }

            weights = weightList.ToArray();
            T = weights.Length;
            this.ground = ground;
        }


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
}
