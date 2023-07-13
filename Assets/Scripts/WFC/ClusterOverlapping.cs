using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Helper;

public class ClusterOverlapping : NewModel
{
    //Dictionary<string, List<byte[]>> patterns;      //Node or Clustername to Patterns
    Dictionary<string, List<int>> colors;
    //List<int> colors;                               //Eine Liste an Farben f�r alle Nodes

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
        propagator = new();

        nodeNames = new();
        T = new();

        Dictionary<Color32, string> inputFieldColors = new();

        foreach (var node in nodes)
        {
            nodeNames.Add(node.Key);
            inputFieldColors.Add(node.Value.NodeColor, node.Key);
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
                }

                if (k == nodeColors.Count) nodeColors.Add(color);
                nodeSample[i] = (byte)k;
            }

            //samples.Add(node.Key, nodeSample);
            colors.Add(node.Key, nodeColors);

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

                        //Pattern wird gehashed anhand dessen Inhalt. Gibt es schon ein Pattern mit demselben Hashwert, so ist es ein duplikat und wird nicht zur Liste hinzugef�gt.
                        byte[] p = ps[k];
                        long h = hash(p, C);
                        if (patternIndices.TryGetValue(h, out int index)) weightList[index] = weightList[index] + 1;    //Umso �fter ein Pattern vorkam, desto h�her steigt die gewichtung (weight) von diesem
                        else
                        {
                            patternIndices.Add(h, weightList.Count);
                            weightList.Add(1.0);
                            nodePatterns.Add(p);
                        }
                    }
                }
            }
            patterns.Add(node.Key, nodePatterns);

            weights.Add(node.Key, weightList.ToArray());     //Gewichtung, wie oft ein pattern vorkam
            T.Add(node.Key, weightList.Count);
            this.ground = ground;

            //get all patterns and save them for each node (needs all colors to calculate hashes)

            //create propagator with clusterMap in mind

            int[][][] nodePropagator = new int[4][][];
            for (int d = 0; d < 4; d++)
            {
                nodePropagator[d] = new int[T[node.Key]][];   //T ist die Anzahl an einmaligen Patterns
                for (int t = 0; t < T[node.Key]; t++)
                {
                    List<int> list = new();
                    for (int t2 = 0; t2 < T[node.Key]; t2++)
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

        //preparing lookup for user given input field
        List<string> inputFieldList = new();

        foreach(Color32 tile in clusterMap.GetPixels32())
        {
            if(inputFieldColors.TryGetValue(tile, out string nodeName))
            {
                inputFieldList.Add(nodeName);
            }
            else
            {
                inputFieldList.Add("root");
            }
        }

        inputField = inputFieldList.ToArray();
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
    /// Funktion die �berpr�ft, ob zwei Patterns an bestimmten Stellen �bereinstimmen
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

    public int[] GenerateBitmap()
    {
        int[] bitmap = new int[MX * MY];
        if (observed[0] >= 0)
        {
            for (int y = 0; y < MY; y++)
            {
                int dy = y < MY - N + 1 ? 0 : N - 1;
                for (int x = 0; x < MX; x++)
                {
                    int dx = x < MX - N + 1 ? 0 : N - 1;

                    string nodeName = inputField[x + y * MX];

                    var nodeColors = colors[nodeName];
                    var nodePatterns = patterns[nodeName];
                    var currentObserved = observed[x - dx + (y - dy) * MX];
                    var currentPosition = dx + dy * N;

                    bitmap[x + y * MX] = nodeColors[nodePatterns[currentObserved][currentPosition]];
                }
            }
        }
        else
        {
            for (int i = 0; i < wave.Length; i++)
            {
                int contributors = 0, r = 0, g = 0, b = 0;
                int x = i % MX, y = i / MX;
                for (int dy = 0; dy < N; dy++) for (int dx = 0; dx < N; dx++)
                    {
                        int sx = x - dx;
                        if (sx < 0) sx += MX;

                        int sy = y - dy;
                        if (sy < 0) sy += MY;

                        int s = sx + sy * MX;
                        if (!periodic && (sx + N > MX || sy + N > MY || sx < 0 || sy < 0)) continue;
                        string nodeName = inputField[s];
                        for (int t = 0; t < T[nodeName]; t++) if (wave[s][t])
                            {
                                contributors++;
                                int argb = colors[nodeName][patterns[nodeName][t][dx + dy * N]];
                                r += (argb & 0xff0000) >> 16;
                                g += (argb & 0xff00) >> 8;
                                b += argb & 0xff;
                            }
                    }
                bitmap[i] = unchecked((int)0xff000000 | ((r / contributors) << 16) | ((g / contributors) << 8) | b / contributors);
            }
        }

        return bitmap;
    }
}
