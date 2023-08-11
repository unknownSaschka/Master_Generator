﻿// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Helper;
//using UnityEngine;

public abstract class NewModel
{
    //------------ADDITIONS-----------------------

    //protected Dictionary<string, List<int>> patternLibrary;
    protected List<string> nodeNames;
    protected string[] inputField;
    protected Dictionary<string, List<int>> clusterPatterns;   //StartIndex, Element Count (used for pattern and weights)
    protected int globalPatternCount;

    //--------------------------------------------

    //[Position im Feld][Anzahl aller Patterns]
    //protected bool[][] wave;            //Beinhaltet für das feld alle noch möglichen Tiles
    protected Dictionary<int, bool>[] wave;

    // [Himmelsrichtung][Anzahl aller Patterns][Anzahl kompatibler Patterns in diese Himmelsrichtung] PatternID
    //protected int[][][] propagator;     //Wie eine LookUp table: Welche Tiles sind in die jeweilige Richtung das bestimmte Tile erlaubt
    protected Dictionary<string, int[][][]> propagator;

    //[Position im Feld][Anzahl aller Patterns][Himmelsrichtung] Farbwert
    int[][][] compatible;               //Anzahl der noch kompatiblen Patterns. Wie viele Patterns sind an einer bestimmten Stelle mit einem bestimmten Pattern in eine bestimmte Himmelsrichtung noch kompatibel

    //[Anzahl der Felder] Tile ID
    protected int[] observed;           //Nachdem der WFC durchgelaufen ist, werden für jede Position das resultiernde Pattern gesetzt

    //[Anzahl Positionen mal Anzahl Pattern] (Position Feld, PatternID)
    //(int, int)[] stack;                 //Stack, auf den alle noch zu prüfenden Tiles (Position mit PatternID) draufgeschoben werden
    List<(int, int)> stack;
    //int stacksize, observedSoFar;
    int observedSoFar;

    protected int MX, MY, N;         //T ist die Anzahl an Patterns, N ist Patterngröße
    protected Dictionary<string, int> T;
    protected bool periodic, ground;    //periodic: wrapping um den Rand des Feldes herum, ground: ?

    //[Anzahl aller Patterns]
    //protected double[] weights;         //Gewichtung aller jeweiligen Patterns. Umso öfter ein Pattern beim Sample vorkam, desto höher ist die Gewichtung
    //protected double[] weights;             //evtl auch als Dictionary<string, Dictionary<int, double>> weights ?
    //double[] weightLogWeights, distribution;    //Gewichtete Gewichtsverteilung
    protected Dictionary<string, Dictionary<int, double>> weightLogWeights;
    //protected Dictionary<string, double[]> distribution;
    protected Dictionary<string, Dictionary<int, double>> weights;
    protected Dictionary<string, Dictionary<int, double>> distribution;

    //[Position im Feld] Länge des Gewichtungs Arrays
    protected int[] sumsOfOnes;         //Für jedes Pattern, das auf einer Position gebannt wird, wird die Variable um eins herunter gezählt, da sich die verteilung auch ändert

    Dictionary<string, double> sumOfWeights;
    Dictionary<string, double> sumOfWeightLogWeights;
    Dictionary<string, double> startingEntropy;        //WICHTIG: Diese wichtig pro Node oder geht global?

    //[Positionen im Feld]
    protected double[] sumsOfWeights;
    protected double[] sumsOfWeightLogWeights;
    protected double[] entropies;

    
    Heuristic heuristic;

    protected NewModel(int width, int height, int N, bool periodic, Heuristic heuristic)
    {
        MX = width;
        MY = height;
        this.N = N;
        this.periodic = periodic;
        this.heuristic = heuristic;
    }

    void Init()
    {
        wave = new Dictionary<int, bool>[MX * MY];                 //erste Dimension sind alle Tiles des Feldes
        //Diese immer pro Pixel von Input Grafik ziehen
        compatible = new int[wave.Length][][];      //erste Dimension auch alle Tiles des Feldes
        distribution = new();
        weightLogWeights = new();
        sumOfWeights = new();
        sumOfWeightLogWeights = new();
        observed = new int[MX * MY];
        startingEntropy = new();

        for (int i = 0; i < wave.Length; i++)
        {
            string nodeName = inputField[i];
            //wave[i] = new bool[T[nodeName]];                  //zweite Dimension sind alle Patterns die existieren
            //wave[i] = new bool[globalPatternCount];                  //zweite Dimension sind alle Patterns die existieren
            wave[i] = new Dictionary<int, bool>();
            //compatible[i] = new int[T[nodeName]][];           //auch hier in der zweiten Dimension alle existierenden Tiles
            compatible[i] = new int[globalPatternCount][];           //auch hier in der zweiten Dimension alle existierenden Tiles

            /**
            for (int t = 0; t < T[nodeName]; t++)
            {
                compatible[i][t] = new int[4];
            }
            */

            for (int t = 0; t < globalPatternCount; t++)
            {
                compatible[i][t] = new int[4];
            }
        }

        foreach (string nodeName in nodeNames)
        {
            //distribution = new double[T];
            distribution.Add(nodeName, new());

            //weightLogWeights = new double[T];
            //sumOfWeights = 0;
            //sumOfWeightLogWeights = 0;
            weightLogWeights.Add(nodeName, new());
            sumOfWeights.Add(nodeName, 0);
            sumOfWeightLogWeights.Add(nodeName, 0);

            foreach (int t in clusterPatterns[nodeName])
            {
                //weightLogWeights[nodeName][t] = weights[nodeName][t] * Math.Log(weights[nodeName][t]);
                weightLogWeights[nodeName].Add(t, weights[nodeName][t] * Math.Log(weights[nodeName][t]));

                sumOfWeights[nodeName] += weights[nodeName][t];
                sumOfWeightLogWeights[nodeName] += weightLogWeights[nodeName][t];
            }


            startingEntropy.Add(nodeName, Math.Log(sumOfWeights[nodeName]) - sumOfWeightLogWeights[nodeName] / sumOfWeights[nodeName]);
        }

        sumsOfOnes = new int[MX * MY];
        sumsOfWeights = new double[MX * MY];
        sumsOfWeightLogWeights = new double[MX * MY];
        entropies = new double[MX * MY];

        //stack = new (int, int)[wave.Length * T];
        //stacksize = 0;
        stack = new();
    }

    public bool Run(int seed, int limit)
    {
        if (wave == null) Init();

        Clear();
        System.Random random = new(seed);

        //Limit ist optional. Wurde keines gesetzt, so ist es -1 also so gesehen unendlich tries
        for (int l = 0; l < limit || limit < 0; l++)
        {
            int node = NextUnobservedNode(random);

            //Solange es eine node gibt, die noch unaufgelöst ist, wird weiter Obvserved und Propagiert.
            if (node >= 0)
            {
                Observe(node, random);
                bool success = Propagate();
                if (!success) return false;
            }
            //gibt es keine weiteren Nodes mehr, so werden die ausgewählten Tiles in wave in das observed Array übertragen. Aus observed wird letzendlich die Bitmap erstellt.
            else
            {
                for (int i = 0; i < wave.Length; i++)
                {
                    string nodeName = inputField[i];
                    /*
                    for (int t = 0; t < T[nodeName]; t++)
                    {
                        if (wave[i][t]) 
                        { 
                            observed[i] = t;    //Alle übrig resultierenden Tiles in wave werden in observed übertragen
                            break; 
                        }
                    }
                    */

                    foreach(int patternID in clusterPatterns[nodeName])
                    {
                        if (wave[i][patternID])
                        {
                            observed[i] = patternID;
                            break;
                        }
                    }
                }
                return true;
            }
        }

        return true;
    }

    public void InitStepRun()
    {
        if (wave == null) Init();

        Clear();
        //Random random = new(seed);
    }

    public int StepRun(System.Random random)
    {
        int node = NextUnobservedNode(random);

        //Solange es eine node gibt, die noch unaufgelöst ist, wird weiter Obvserved und Propagiert.
        if (node >= 0)
        {
            Observe(node, random);
            bool success = Propagate();
            if (!success) return -1;
        }
        //gibt es keine weiteren Nodes mehr, so werden die ausgewählten Tiles in wave in das observed Array übertragen. Aus observed wird letzendlich die Bitmap erstellt.
        else
        {
            for (int i = 0; i < wave.Length; i++)
            {
                string nodeName = inputField[i];
                for (int t = 0; t < T[nodeName]; t++)
                {
                    if (wave[i][t])
                    {
                        observed[i] = t;    //Alle übrig resultierenden Tiles in wave werden in observed übertragen
                        break;
                    }
                }
            }
            return 0;
        }

        return 1;
    }

    /// <summary>
    /// Gibt das Feld mit der niedrigsten Entropie zurück.
    /// </summary>
    /// <param name="random"></param>
    /// <returns></returns>
    int NextUnobservedNode(System.Random random)
    {
        if (heuristic == Heuristic.Scanline)
        {
            for (int i = observedSoFar; i < wave.Length; i++)
            {
                if (!periodic && (i % MX + N > MX || i / MX + N > MY)) continue;
                if (sumsOfOnes[i] > 1)
                {
                    observedSoFar = i + 1;
                    return i;
                }
            }
            return -1;
        }

        double min = 1E+4;
        int argmin = -1;

        //Geht durch das gesamte Feld und berechnet die Entropie. Dazu wird noise auf die Entropy addiert. Das Feld mit ner niedrigsten Entropy wird zurückgegeben
        for (int i = 0; i < wave.Length; i++)
        {
            if (!periodic && (i % MX + N > MX || i / MX + N > MY)) continue;
            int remainingValues = sumsOfOnes[i];
            double entropy = heuristic == Heuristic.Entropy ? entropies[i] : remainingValues;
            if (remainingValues > 1 && entropy <= min)
            {
                double noise = 1E-6 * random.NextDouble();
                if (entropy + noise < min)
                {
                    min = entropy + noise;
                    argmin = i;
                }
            }
        }
        //Debug.Log(argmin);
        return argmin;
    }

    void Observe(int node, System.Random random)       //node = Position im Feld
    {
        //bool[] w = wave[node];
        Dictionary<int, bool> w = wave[node];
        string nodeName = inputField[node];


        /*
        for (int t = 0; t < ; t++)
        { 
            distribution[nodeName][t] = w[t] ? weights[t] : 0.0;      //Setzt die Verteilung aller noch möglichen Tiles in ein neues Array
            //evtl. falls es das Pattern in dem Cluster nicht gibt und dennoch drüberiteriert wird, einfach -1 sagen?
        }
        */

        foreach(var weight in weights[nodeName])
        {
            if (!distribution[nodeName].ContainsKey(weight.Key)) 
            {
                distribution[nodeName].Add(weight.Key, 0.0);
            }

            distribution[nodeName][weight.Key] = w[weight.Key] ? weight.Value : 0.0;
        }

        int r = distribution[nodeName].Random(random.NextDouble());                           //Nächstes Pattern welches gesetzt werrden soll

        /*
        for (int t = 0; t < T[nodeName]; t++)
        {
            if (w[t] != (t == r))                                                   //Alle anderen Tiles außer dem auserwählten Tile werden gebannt, sodass nur noch das Auserwählte Tile übrig bleibt
            {
                Ban(node, t);
            }
        }
        */

        foreach(int patternID in clusterPatterns[nodeName])
        {
            if (w[patternID] != (patternID == r))
            {
                Ban(node, patternID);
            }
        }
    }

    bool Propagate()
    {
        while (stack.Count > 0)       //So lange alle unresolved tiles abarbeiten bis keine mehr vorhanden sind
        {
            (int i1, int t1) = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            //stacksize--;

            //x und y Koordiante aus 1-Dimensionalem Array bestimmen
            int x1 = i1 % MX;
            int y1 = i1 / MX;

            string currentNodeName = inputField[i1];

            //ADDITION Master Thesis M1
            //wenn das benachbarte Tile zu einem anderen CLuster gehört, dieses genauso überspringen als wenn es außerhalb der Map wäre

            //prüft alle 4 umliegenden Tiles
            for (int d = 0; d < 4; d++)
            {
                int x2 = x1 + dx[d];
                int y2 = y1 + dy[d];
                if (!periodic && (x2 < 0 || y2 < 0 || x2 + N > MX || y2 + N > MY)) continue;        //Falls außerhalb der Boundary, entweder ignorieren (continue) oder wrapping

                if (x2 < 0) x2 += MX;
                else if (x2 >= MX) x2 -= MX;
                if (y2 < 0) y2 += MY;
                else if (y2 >= MY) y2 -= MY;

                
                int i2 = x2 + y2 * MX;              //2-dim position wieder in 1-dim position umwandeln
                string neighbourNodeName = inputField[i2];
                //if (neighbourNodeName != currentNodeName) continue;                                 //Falls das benachbarte Feld zu einem anderen Cluster gehört, vorerst überspringen

                //current node name ist identisch mit dem nachbar, also kann ich dieses einfach weiter laufen lassen
                int[] p = propagator[currentNodeName][d][t1];        //holt sich alle möglichen Teile für diese Konstellation und das spezifische Tile heraus


                //TESTING: if p == 0 skip, because pattern isn't available
                if (p == null) continue;

                int[][] compat = compatible[i2];    

                for (int l = 0; l < p.Length; l++)
                {
                    int t2 = p[l];
                    int[] comp = compat[t2];

                    //Auch hier, wenns das Pattern generell nicht gibt, überspringen erstmal
                    if(comp == null) continue;

                    comp[d]--;
                    if (comp[d] == 0) Ban(i2, t2);
                }
            }
        }

        return sumsOfOnes[0] > 0;
    }

    void Ban(int i, int t)      //i = Position im Feld, t = PatternID
    {
        wave[i][t] = false;

        int[] comp = compatible[i][t];

        if (comp == null) return;       //ID ist nicht verfügbar in diesem Set (Cluster), also kann es auch nicht gebannt werden

        for (int d = 0; d < 4; d++) comp[d] = 0;
        //stack[stacksize] = (i, t);                  //Für jedes Tile das gebannt wurde, wird dieses auf den Stack geschoben, da alle angrenzenden Tiles nun auch geprüft werden müssen
        //stacksize++;
        stack.Add((i, t));
        string nodeName = inputField[i];

        sumsOfOnes[i] -= 1;
        sumsOfWeights[i] -= weights[nodeName][t];
        sumsOfWeightLogWeights[i] -= weightLogWeights[nodeName][t];

        double sum = sumsOfWeights[i];
        entropies[i] = Math.Log(sum) - sumsOfWeightLogWeights[i] / sum;
    }

    void Clear()
    {
        for (int i = 0; i < wave.Length; i++)
        {
            string nodeName = inputField[i];
            /*
            for (int t = 0; t < T[nodeName]; t++)
            {
                wave[i][t] = true;
                for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;    //Speichere die Menge an noch kompatiblen Tiles in diese Himmelsrichtung ab
            }
            */

            //ADDITION
            foreach(var t in clusterPatterns[nodeName])
            {
                //wave[i][t] = true;      //Wird behandelt: Wenn wave null ist, dann gibt es dieses pattern für den Cluster vorerst gar nicht
                if (wave[i].ContainsKey(t))
                {
                    wave[i][t] = true;
                }
                else
                {
                    wave[i].Add(t, true);
                }

                for(int d = 0; d < 4; d++)
                {
                    compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;      //Speichere die Menge an noch kompatiblen Tiles in diese Himmelsrichtung ab
                }
                
            }


            sumsOfOnes[i] = T[nodeName];
            sumsOfWeights[i] = sumOfWeights[nodeName];
            sumsOfWeightLogWeights[i] = sumOfWeightLogWeights[nodeName];
            entropies[i] = startingEntropy[nodeName];
            observed[i] = -1;
        }
        observedSoFar = 0;

        if (ground)
        {
            for (int x = 0; x < MX; x++)
            {

                int mxy1 = x + (MY - 1) * MX;
                string nodeName1 = inputField[mxy1];
                /*
                for (int t = 0; t < T[nodeName1] - 1; t++)
                {
                    Ban(mxy1, t);
                }
                */

                foreach(int patternID in clusterPatterns[nodeName1].SkipLast(1))
                {
                    Ban(mxy1, patternID);
                }

                for (int y = 0; y < MY - 1; y++) 
                {
                    int mxy2 = x + y * MX;
                    string nodeName2 = inputField[mxy2];
                    Ban(mxy2, clusterPatterns[nodeName2].Last() - 1); 
                }
            }
            Propagate();
        }
    }

    protected Dictionary<int, double> GetWeightsOfNode(string nodeName)
    {
        Dictionary<int, double> weightDic = new();

        foreach(var patternID in clusterPatterns[nodeName])
        {
             weightDic.Add(patternID, weights[nodeName][patternID]);
        }

        return weightDic;
    }

    public abstract void Save(string filename);

    //implementierung um die Tiles eines Tile herum zu schauen (links, oben, rechts, unten)
    protected static int[] dx = { -1, 0, 1, 0 };
    protected static int[] dy = { 0, 1, 0, -1 };
    static int[] opposite = { 2, 3, 0, 1 };     //was ist die entgegenliegende Tile position
}
