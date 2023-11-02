using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Helper;

public class ClusterOverlapping : NewModel
{
    //Dictionary<string, List<byte[]>> patterns;      //Node or Clustername to Patterns
    //>Dictionary<string, List<int>> colors;
    List<int> colors;                               //Eine Liste an Farben. Farben werden global betrachtet

    //List<byte[]> patterns;                          //global/all patterns
    //List<byte[]> patterns;        //per node patterns. Speichert die colorID zu den einzelnen Farben der Elemente des Patterns ab
    Dictionary<int, byte[]> patterns;
    


    public ClusterOverlapping(Dictionary<string, Node> nodes, Texture2D clusterMap, int N, int width, int height, bool periodic, bool ground, Heuristic heuristic, 
        ExtendedHeuristic extendedHeuristic, CompatibleInit compatibleInit, int backtrackTries, bool backtracking, bool clusterBanning, bool banLowerClusterInRoot, string patternOutputFolder)
        : base(width, height, N, periodic, heuristic, extendedHeuristic, compatibleInit, backtrackTries, backtracking, clusterBanning, banLowerClusterInRoot)
    {
        //load all samples for each node
        //Dictionary<string, byte[]> samples = new();
        colors = new();
        patterns = new();
        weights = new();
        propagator = new();

        clusterPatterns = new();
        nodeNames = new();
        T = new();

        nodeDepth = new();
        hasNodeSample = new();

        Dictionary<Color32, string> inputFieldColors = new();

        List<KeyValuePair<string, Node>> toProcess = new();

        Dictionary<string, byte[]> nodeSamples = new();

        //TESTING
        Dictionary<long, int> patternIndices = new();

        //List<double> weightList = new();

        //int patternIndex = 0;

        foreach(var node in nodes)
        {
            if (node.Value.Sample == null) continue;         //When node has no sample, its a combined node from other nodes

            int[] nodeBitmap = node.Value.Sample.GetBitmap();
            byte[] nodeSample = new byte[nodeBitmap.Length];

            for (int i = 0; i < nodeSample.Length; i++)
            {
                int color = nodeBitmap[i];
                int k = 0;
                for (; k < colors.Count; k++)
                {
                    if (colors[k] == color) break;
                }

                if (k == colors.Count) colors.Add(color);
                nodeSample[i] = (byte)k;
            }

            nodeSamples.Add(node.Key, nodeSample);
        }

        foreach (var node in nodes)
        {
            nodeNames.Add(node.Key);
            nodeDepth.Add(node.Key, node.Value.Depth);

            hasNodeSample.Add(node.Key, node.Value.Sample != null);         //When node has no sample, its a combined node from other nodes

            if (node.Value.PrototypePlaceable) inputFieldColors.Add(node.Value.NodeColor, node.Key);     //Alle verbindungspatterns sind nicht placeable, haben jedoch ein Sample

            //Sort the normal Nodes from Leafs and process parent nodes later when all patterns from the leafs are prepared
            if (node.Value.Sample == null)
            {
                toProcess.Add(node);
                continue;
            }

            bool periodicInput = node.Value.Periodic;
            int symmetry = node.Value.Symmetry;
            
            //int[] nodeBitmap = node.Value.Sample.GetBitmap();
            int sx = node.Value.Sample.width;
            int sy = node.Value.Sample.height;

            //List<int> nodeColors = new();


            //get all colors from all samples and prepare indexed sample array for each node
            byte[] nodeSample = nodeSamples[node.Key];

            //samples.Add(node.Key, nodeSample);
            //colors.Add(node.Key, nodeColors);

            Dictionary<int, byte[]> nodePatterns = new();
            //Dictionary<long, int> patternIndices = new();
            Dictionary<int, double> nodeWeightDic = new();
            //List<int> patternIndexList = new();

            int C = colors.Count;       //wichtig für hashing algorithmus. Sollte keine große Auswirkung haben wenn ungenutzte Farben auch dabei sind
            //int C = 10;
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
                        if (patternIndices.TryGetValue(h, out int index))
                        {
                            if (nodePatterns.ContainsKey(index))
                            {
                                nodeWeightDic[index] = nodeWeightDic[index] + 1;    //Umso öfter ein Pattern vorkam, desto höher steigt die gewichtung (weight) von diesem
                            }
                            else
                            {
                                nodeWeightDic.Add(index, 1.0);
                                nodePatterns.Add(index, p);
                            }
                        }
                        else
                        {
                            index = patternIndices.Count;
                            patternIndices.Add(h, index);
                            nodeWeightDic.Add(index, 1.0);
                            nodePatterns.Add(index, p);
                            //patternIndexList.Add(patternIndex);
                            //patternIndex++;
                        }
                    }
                }
            }
            //patterns.Add(node.Key, nodePatterns);

            //Global Pattern indexing
            //patterns.AddRange(nodePatterns);
            //clusterPatterns.Add(node.Key, new List<int>() { (patternIndex, nodeWeightList.Count) });
            //clusterPatterns.Add(node.Key, patternIndexList);
            //patternIndex = patternIndex + nodeWeightList.Count;

            //weights.Add(node.Key, weightList.ToArray());     //Gewichtung, wie oft ein pattern vorkam
            //weightList.AddRange(nodeWeightList);
            //weights.Add(node.Key, nodeWeightDic);
            List<int> indices = new();
            foreach (var pattern in nodePatterns)
            {
                indices.Add(pattern.Key);
                if (patterns.ContainsKey(pattern.Key)) continue;
                patterns.Add(pattern.Key, pattern.Value);
            }

            clusterPatterns.Add(node.Key, indices);
            weights.Add(node.Key, nodeWeightDic);
            
            T.Add(node.Key, nodeWeightDic.Count);
            this.ground = ground;

            //get all patterns and save them for each node (needs all colors to calculate hashes)

            //create propagator with clusterMap in mind

            //PreparePropagator(node.Key, nodePatterns);
        }

        //weights = weightList.ToArray();

        //Propagator preparing for each leafs. Parent Nodes won't be in the dictionary by now
        foreach(var cluster in clusterPatterns)
        {
            Dictionary<int, byte[]> patternDic = new();

            foreach(var i in cluster.Value)
            {
                patternDic.Add(i, patterns[i]);
            }

            PreparePropagator(cluster.Key, patternDic);
        }

        while(toProcess.Count > 0)
        {
            foreach (var node in toProcess.ToArray())
            {
                if (Combine(node.Key, node.Value.Children)) 
                { 
                    toProcess.Remove(node); 
                }
            }
        }


        //preparing lookup for user given input field
        List<string> inputFieldList = new();

        Color32[] pixels = clusterMap.GetPixels32().FlipVertically(clusterMap.width, clusterMap.height);        //FlipVertically because Unity Texture2D starts lower left but WFC starts upper left
        inputField = new string[clusterMap.width * clusterMap.height];

        
        foreach (Color32 tile in pixels)
        {
            if(inputFieldColors.TryGetValue(tile, out string nodeName))
            {
                inputFieldList.Add(nodeName);
            }
        }
        

        //inputField = new int[clusterMap.width * clusterMap.height];

        /*
        for(int i = 0; i < pixels.Length; i++)
        {
            int x = i % clusterMap.width;
            int y = i / clusterMap.height;

            int newY = clusterMap.height - y - 1;

            inputField[x + newY * clusterMap.height] = inputFieldColors[pixels[i]];
        }
        */

        inputField = inputFieldList.ToArray();
        globalPatternCount = patterns.Count;


        //Print Pattern Counts
        string output = "Generated Pattern amount: \r\n";
        foreach(var t in T)
        {
            output += $"{t.Key}: {t.Value}\r\n";
        }
        UnityEngine.Debug.Log(output);
        SavePatterns(patternOutputFolder);
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

    public int[] GenerateBitmap()
    {
        int[] bitmap = new int[MX * MY];

        if(observed == null)
        {
            Debug.Log("Not finished yet.");
            return null;
        }

        if (observed[0] >= 0)
        {
            Debug.Log("Generate Bitmap for Finished");
            for (int y = 0; y < MY; y++)
            {
                //int dy = 0;
                int dy = y < MY - N + 1 ? 0 : N - 1;
                for (int x = 0; x < MX; x++)
                {
                    //int dx =  0;
                    int dx = x < MX - N + 1 ? 0 : N - 1;

                    //string nodeName = inputField[x + y * MX];

                    //var nodeColors = colors[nodeName];
                    //var nodePatterns = patterns[nodeName];
                    var currentObserved = observed[x - dx + (y - dy) * MX];
                    var currentPosition = dx + dy * N;

                    bitmap[x + y * MX] = 0;

                    //TODO: Backtracking einführen, da wegen Contradiction teils keine Teile generiert werden können
                    if (currentObserved < patterns.Count && currentObserved >= 0)
                    {
                        byte[] p = patterns[currentObserved];
                        byte s = p[currentPosition];
                        bitmap[x + y * MX] = colors[s];
                    }
                    else
                    {
                        bitmap[x + y * MX] = 0;
                    }
                    
                }
            }
        }
        else
        {
            Debug.Log("Generate Bitmap for Unfinished");
            for (int i = 0; i < wave.Length; i++)
            {
                int contributors = 0, r = 0, g = 0, b = 0;
                int x = i % MX, y = i / MX;
                string nodeName = inputField[i];
                for (int dy = 0; dy < N; dy++) for (int dx = 0; dx < N; dx++)
                    {
                        int sx = x - dx;
                        if (sx < 0) sx += MX;

                        int sy = y - dy;
                        if (sy < 0) sy += MY;

                        int s = sx + sy * MX;
                        if (!periodic && (sx + N > MX || sy + N > MY || sx < 0 || sy < 0)) continue;
                        string neighbourNodeName = inputField[s];
                        foreach(var entry in wave[s])
                        {
                            if (entry.Value)
                            {
                                if (!clusterPatterns[nodeName].Contains(entry.Key)) continue;

                                contributors++;
                                int argb = colors[patterns[entry.Key][dx + dy * N]];
                                r += (argb & 0xff0000) >> 16;
                                g += (argb & 0xff00) >> 8;
                                b += argb & 0xff;
                            }
                        }
                        /*
                        for (int t = 0; t < globalPatternCount; t++)
                        {
                            if (wave[s][t])
                            {
                                contributors++;
                                int argb = colors[patterns[t][dx + dy * N]];
                                r += (argb & 0xff0000) >> 16;
                                g += (argb & 0xff00) >> 8;
                                b += argb & 0xff;
                            }
                        }
                        */
                    }

                if(contributors == 0)
                {
                    bitmap[i] = 0;
                }
                else
                {
                    bitmap[i] = unchecked((int)0xff000000 | ((r / contributors) << 16) | ((g / contributors) << 8) | b / contributors);
                }
            }

            if (preDecided != null)
            {
                //------------------ADDDITION TESTING-----------------------
                for (int y = 0; y < MY; y++)
                {
                    //int dy = 0;
                    int dy = y < MY - N + 1 ? 0 : N - 1;
                    for (int x = 0; x < MX; x++)
                    {
                        //int dx =  0;
                        int dx = x < MX - N + 1 ? 0 : N - 1;

                        if (!preDecided[x - dx + (y - dy) * MX]) continue;

                        //string nodeName = inputField[x + y * MX];

                        //var nodeColors = colors[nodeName];
                        //var nodePatterns = patterns[nodeName];
                        var currentObserved = observed[x - dx + (y - dy) * MX];
                        var currentPosition = dx + dy * N;

                        bitmap[x + y * MX] = 0;

                        //TODO: Backtracking einführen, da wegen Contradiction teils keine Teile generiert werden können
                        if (currentObserved < patterns.Count && currentObserved >= 0)
                        {
                            byte[] p = patterns[currentObserved];
                            byte s = p[currentPosition];
                            bitmap[x + y * MX] = colors[s];
                        }
                        else
                        {
                            bitmap[x + y * MX] = 0;
                        }

                    }
                }
            }
        
        }

        return bitmap;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nodeNames"></param>
    /// <returns>Returns true when combining was successful. Returns false when one or more nodes were'nt initialized jet.</returns>
    public bool Combine(string clusterNodeName, List<string> nodeNames)
    {
        //TODOs:

        //List<int> nodeColors = new();
        //double[] nodeWeights;
        //List<double> nodeWeights = new();


        //combine propagator and patterns
        //List<byte[]> patternList = new();
        //int patternCount = 0;

        foreach(string nodeName in nodeNames)
        {
            //patternCount (T)
            //Weights
            //Propagator preparing


            //Patterns wurden bisher noch nicht initialisiert
            if (!clusterPatterns.ContainsKey(nodeName))
            {
                return false;
            }
        }



        //Prepare Dictionary for all PatternID Lists
        List<int> patternList = new();
        Dictionary<int, double> weight = new();

        foreach(string nodeName in nodeNames)
        {
            //patternList.AddRange(clusterPatterns[nodeName]);
            foreach(int patternID in clusterPatterns[nodeName])
            {
                if (!patternList.Contains(patternID))
                {
                    patternList.Add(patternID);
                }
            }
            
            foreach(var entry in weights[nodeName])
            {
                if(!weight.TryAdd(entry.Key, entry.Value))
                {
                    weight[entry.Key] += entry.Value;   //Sollte das Pattern schon vorhanden sein, so soll die Gewichtung addiert werden
                }
            }
        }

        clusterPatterns.Add(clusterNodeName, patternList);

        Dictionary<int, byte[]> patternDic = new();

        foreach(var i in patternList)
        {
            patternDic.Add(i, patterns[i]);
        }

        T.Add(clusterNodeName, patternList.Count);
        weights.Add(clusterNodeName, weight);
        

        //T.Add(clusterNodeName, patternCount);
        //patterns.Add(clusterNodeName, patternList);
        //colors.Add(clusterNodeName, nodeColors);
        //weights.Add(clusterNodeName, nodeWeights.ToArray());

        PreparePropagator(clusterNodeName, patternDic);

        return true;
    }

    private void PreparePropagator(string nodeName, Dictionary<int, byte[]> nodePatterns)
    {
        //TODO: Nochmal checken ob Propagator richtig aufgesetzt wird

        int[][][] nodePropagator = new int[4][][];
        for (int d = 0; d < 4; d++)
        {
            //nodePropagator[d] = new int[T[nodeName]][];
            nodePropagator[d] = new int[patterns.Count][];
            foreach(var t in nodePatterns)
            {
                List<int> list = new();
                foreach(var t2 in nodePatterns)
                {
                    if (agrees(t.Value, t2.Value, dx[d], dy[d], N))
                    {
                        list.Add(t2.Key);
                    }
                }

                nodePropagator[d][t.Key] = new int[list.Count];   //Hier die Frage, ob größe von allen Patterns und nur die erlaubten PatternIDs dann hinterlegt sind
                //nodePropagator[d][t.Key] = new int[];

                for(int c = 0; c < list.Count; c++)
                {
                    nodePropagator[d][t.Key][c] = list[c];
                }
            }
        }

        propagator.Add(nodeName, nodePropagator);

        /*
        int[][][] nodePropagator = new int[4][][];
        for (int d = 0; d < 4; d++)
        {
            nodePropagator[d] = new int[T[nodeName]][];   //T ist die Anzahl an einmaligen Patterns
            for (int t = 0; t < T[nodeName]; t++)
            {
                List<int> list = new();
                for (int t2 = 0; t2 < T[nodeName]; t2++)
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
        propagator.Add(nodeName, nodePropagator);
        */
    }

    public void SavePatterns(string patternOutputFolder)
    {
        //Dictionary<int, int[]> translatedPatterns = new();

        foreach (var pat in patterns)
        {
            int patternID = pat.Key;
            int[] patternWithColorInt = new int[N*N];

            byte[] p = patterns[patternID];

            for (int i = 0; i < N*N; i++)
            {
                byte s = p[i];
                patternWithColorInt[i] = colors[s];
            }

            
            //string ImageOutputFolder = "E:\\Studium\\Masterarbeit\\WFC Testing\\simple\\Graph\\patterns\\";
            if (!File.Exists($"{patternOutputFolder}{patternID}.png"))
            {
                Texture2D result = GetTextureFromInt(patternWithColorInt, true);
                var texturePNG = result.EncodeToPNG();
                File.WriteAllBytes($"{patternOutputFolder}{patternID}.png", texturePNG);
            }
        }
    }

    //copied frpm WFC.cs
    private Texture2D GetTextureFromInt(int[] bitmap, bool flip)
    {
        Color32[] colors = new Color32[bitmap.Length];
        for (int i = 0; i < bitmap.Length; i++)
        {
            byte[] cols = BitConverter.GetBytes(bitmap[i]);
            colors[i] = new Color32(cols[2], cols[1], cols[0], cols[3]);        //WFC saves in BGRA
        }

        Texture2D resultTexture = new Texture2D(N, N, TextureFormat.RGBA32, false);
        if (flip)
        {
            resultTexture.SetPixels32(colors.FlipVertically(N, N));
        }
        else
        {
            resultTexture.SetPixels32(colors);
        }

        resultTexture.filterMode = FilterMode.Point;
        resultTexture.Apply();

        return resultTexture;
    }
}
