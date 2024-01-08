// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using Assets.Scripts.WFC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Helper;
using static UnityEditor.PlayerSettings;
using static UnityEngine.EventSystems.EventTrigger;
//using UnityEngine;

public abstract class NewModel
{
    //------------ADDITIONS-----------------------

    //protected Dictionary<string, List<int>> patternLibrary;
    protected List<string> nodeNames;
    protected string[] inputField;
    protected Dictionary<string, List<int>> clusterPatterns;   //StartIndex, Element Count (used for pattern and weights)
    public int globalPatternCount;

    //int remainingNormal = 0;
    List<int> remaining;
    protected Dictionary<string, int> nodeDepth;                //Initialized in ClusterOverlapping
    protected Dictionary<string, bool> hasNodeSample;           //tracks if Node was Combined or generated from a sample. Ture is leaf node, false is combined node

    //Backtracking
    protected List<ModelState> modelStates;
    protected int backtrackTries;
    protected int backtrackTriesCounter;

    public double[] entropySave;
    public double[] entropySaveStepped;
    public Dictionary<int, bool>[] waveSave;

    private int StepCounter;

    //--------------------------------------------

    //[Position im Feld][Anzahl aller Patterns]
    //protected bool[][] wave;            //Beinhaltet für das feld alle noch möglichen Tiles
    public Dictionary<int, bool>[] wave;

    // [Himmelsrichtung][Anzahl aller Patterns][Anzahl kompatibler Patterns in diese Himmelsrichtung] PatternID
    //protected int[][][] propagator;     //Wie eine LookUp table: Welche Tiles sind in die jeweilige Richtung das bestimmte Tile erlaubt
    protected Dictionary<string, int[][][]> propagator;

    //[Position im Feld][Anzahl aller Patterns][Himmelsrichtung] Farbwert
    int[][][] compatible;               //Anzahl der noch kompatiblen Patterns. Wie viele Patterns sind an einer bestimmten Stelle mit einem bestimmten Pattern in eine bestimmte Himmelsrichtung noch kompatibel

    //[Anzahl der Felder] Tile ID
    public int[] observed;           //Nachdem der WFC durchgelaufen ist, werden für jede Position das resultiernde Pattern gesetzt

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
    ExtendedHeuristic extendedHeuristic;
    CompatibleInit compatibleInit;

    //-------MORE ADDITIONS-------

    public int[] preFinishedObserved = null;
    public Dictionary<int, bool>[] preFinishedWave = null;
    protected bool[] preDecided = null;

    bool backtracking;
    bool clusterBanning;
    bool banLowerClusterInRoot;
    List<ModelState> testDebug = new();
    protected bool[] decided;

    private bool initPhase = false;

    private int[] remainingPatternsSum;

    private System.Random _random;

    protected NewModel(int width, int height, int N, bool periodic, Heuristic heuristic, ExtendedHeuristic extendedHeuristic, CompatibleInit compInit,
        int backtrackTries, bool backtracking, bool clusterBanning, bool banLowerClusterInRoot)
    {
        MX = width;
        MY = height;
        this.N = N;
        this.periodic = periodic;
        this.heuristic = heuristic;
        this.extendedHeuristic = extendedHeuristic;
        this.compatibleInit = compInit;
        this.backtrackTries = backtrackTries;
        this.backtracking = backtracking;
        this.clusterBanning = clusterBanning;
        this.banLowerClusterInRoot = banLowerClusterInRoot;
    }

    void Init()
    {
        modelStates = new();
        wave = new Dictionary<int, bool>[MX * MY];                 //erste Dimension sind alle Tiles des Feldes
        //Diese immer pro Pixel von Input Grafik ziehen
        compatible = new int[wave.Length][][];      //erste Dimension auch alle Tiles des Feldes
        distribution = new();
        weightLogWeights = new();
        sumOfWeights = new();
        sumOfWeightLogWeights = new();
        observed = new int[MX * MY];
        startingEntropy = new();

        remaining = new();
        decided = new bool[MX * MY];

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

    public bool Run(System.Random rng, int limit)
    {
        //if (wave == null) Init();
        Init();

        initPhase = true;
        Clear();
        calculateRemaining();
        //CreateBacktrackStep(testDebug);
        if(clusterBanning) InitBan();      //Bans all Patterns which arent possible with neighbouring patterns of other cluster
        //CreateBacktrackStep(testDebug);
        CreateBacktrackStep();
        initPhase = false;
        System.Random random = rng;

        PreBanning();
        entropySave = SaveEntropies();
        calcRemainingSum();
        waveSave = SaveWave();

        //SaveCurrentProgress();

        bool solvable = isSolvable();
        if (!solvable)
        {
            UnityEngine.Debug.Log("Unsolvable");
            return false;
        }

        //remainingNormal = -1;

        

        //Limit ist optional. Wurde keines gesetzt, so ist es -1 also so gesehen unendlich tries
        for (int l = 0; l < limit || limit < 0; l++)
        {
            if(entropies != null)
            {
                //UnityEngine.Debug.Log($"Entropy[0]:{entropies[0]}");
            }

            if (backtrackTriesCounter > backtrackTries)
            {
                return false;
            }

            if (remaining.Last() == 0 && preFinishedObserved == null)
            {
                SaveCurrentProgress();
            }

            int node = NextUnobservedNode(random);


            //Solange es eine node gibt, die noch unaufgelöst ist, wird weiter Obvserved und Propagiert.
            if (node >= 0)
            {
                //if (inputField[node].Equals("root")) Debugger.Break();
                Observe(node, random);
                bool success = Propagate();
                //UnityEngine.Debug.Log($"Success: {success}");
                if (!success)
                {
                    UnityEngine.Debug.Log($"Contradiction after {l} steps.");
                    //return false;
                    backtrackTriesCounter++;
                    //UnityEngine.Debug.Log($"BacktrackCounter: {backtrackTriesCounter}");
                    if (backtrackTries < backtrackTriesCounter)
                    {
                        return false;
                    }

                    //first try to go back one step
                    //BacktrackRestore(-1);

                    if (backtracking)
                    {
                        backtrackTriesCounter++;
                        CreateBacktrackStep();
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            
            else if(node == -2)
            {
                backtrackTriesCounter++;
                BacktrackRestore(-1);
                UnityEngine.Debug.Log($"BacktrackCounter: {backtrackTriesCounter}");
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

    public void InitStepRun(System.Random rng)
    {
        Init();
        StepCounter = 0;
        initPhase = true;
        Clear();
        calculateRemaining();
        //CreateBacktrackStep(testDebug);
        if (clusterBanning) InitBan();      //Bans all Patterns which arent possible with neighbouring patterns of other cluster
        //CreateBacktrackStep(testDebug);
        CreateBacktrackStep();
        initPhase = false;
        _random = rng;

        //SaveCurrentProgress();
        PreBanning();
        waveSave = SaveWave();
        entropySave = SaveEntropies();

        bool solvable = isSolvable();
        if (!solvable)
        {
            UnityEngine.Debug.Log("Unsolvable");
            return;
        }
    }

    public bool StepRun()
    {
        int node = NextUnobservedNode(_random);

        StepCounter++;
        UnityEngine.Debug.Log("Step " + StepCounter);

        UnityEngine.Debug.Log(node);

        //Solange es eine node gibt, die noch unaufgelöst ist, wird weiter Obvserved und Propagiert.
        if (node >= 0)
        {
            //if (inputField[node].Equals("root")) Debugger.Break();
            Observe(node, _random);
            bool success = Propagate();

            //waveSave = SaveWave();
            entropySaveStepped = SaveEntropies();
            //UnityEngine.Debug.Log($"Success: {success}");
            if (!success)
            {
                //return false;
                backtrackTriesCounter++;
                //UnityEngine.Debug.Log($"BacktrackCounter: {backtrackTriesCounter}");
                if (backtrackTries < backtrackTriesCounter) return false;

                //first try to go back one step
                //BacktrackRestore(-1);

                if (backtracking)
                {
                    backtrackTriesCounter++;
                    CreateBacktrackStep();
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        return false;

        /*
        else if (node == -2)
        {
            backtrackTriesCounter++;
            BacktrackRestore(-1);
            UnityEngine.Debug.Log($"BacktrackCounter: {backtrackTriesCounter}");
        }

        //gibt es keine weiteren Nodes mehr, so werden die ausgewählten Tiles in wave in das observed Array übertragen. Aus observed wird letzendlich die Bitmap erstellt.
        else
        {
            for (int i = 0; i < wave.Length; i++)
            {
                string nodeName = inputField[i];

                foreach (int patternID in clusterPatterns[nodeName])
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
        */
    }

    public void InitBan()
    {
        for(int i = 0; i < wave.Length; i++)
        {
            string nodeName = inputField[i];
            List<int> currentPatternList = clusterPatterns[nodeName];

            if (!nodeName.Equals("root")) continue;

            for (int d = 0; d < 4; d++)
            {
                int x1 = i % MX;
                int y1 = i / MX;

                int x2 = x1 + dx[d];
                int y2 = y1 + dy[d];
                //if (!periodic && (x2 < 0 || y2 < 0 || x2 + N > MX || y2 + N > MY)) continue;        //Falls außerhalb der Boundary, entweder ignorieren (continue) oder wrapping

                if (x2 < 0) x2 += MX;
                else if (x2 >= MX) x2 -= MX;
                if (y2 < 0) y2 += MY;
                else if (y2 >= MY) y2 -= MY;


                int i2 = x2 + y2 * MX;              //2-dim position des Nachbarn wieder in 1-dim position umwandeln

                string neighbourNodeName = inputField[i2];
                if (nodeName.Equals(neighbourNodeName)) continue;       //Wenn Nachbar vom selben Cluster ist, muss nichts gebannt werden da die Menge der verfügbaren Patterns gleich ist

                HashSet<int> set = new();
                int[][] directionalPatterns = propagator[nodeName][d];

                for (int j = 0; j < globalPatternCount; j++)
                {
                    int[] directionalCompatiblePatternIDs = directionalPatterns[j];

                    bool hasMatch = directionalCompatiblePatternIDs.Intersect(clusterPatterns[neighbourNodeName]).Any();

                    if (!hasMatch)
                    {
                        set.Add(j);
                    }
                }

                //List<int> neighbourPatternList = clusterPatterns[neighbourNodeName];
                //var toBan = currentPatternList.Except(neighbourPatternList);

                foreach(int t in set)
                {
                    Ban(i2, t);
                }
            }
        }

        Propagate();
    }

    /// <summary>
    /// Gibt das Feld mit der niedrigsten Entropie zurück.
    /// </summary>
    /// <param name="random"></param>
    /// <returns></returns>
    int NextUnobservedNode(System.Random random)
    {
        //für heuristische. Wird nicht gebraucht
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

        calculateRemaining();
        int remainingDepth = getNextUndecidedNodeDepth();

        if (remainingDepth == -1)
        {
            return -1;
        }


        /*
         * //remainingNormal = 0;
         * 
        for (int i = 0; i < inputField.Length; i++)
        {
            string entry = inputField[i];
            //bool available = false;
            int trueAmount = 0;

            int x = i % MX;
            int y = i / MX;

            foreach (var l in wave[i])
            {
                if (!(i % MX + N > MX || i / MX + N > MY))
                    if (l.Value) trueAmount++;
            }
            //TESTING
            
            if ((entry.Equals("grass") || entry.Equals("water")) && trueAmount > 1)
            {
                remainingNormal++;
                decided[i] = true;
            }
            

            
            if(trueAmount == 0)
            {
                return -2;      //If an undecidable piece exist, return -2
            }
            
        }
        //UnityEngine.Debug.Log($"RemainingNormal:{remainingNormal}");
        */


        //Geht durch das gesamte Feld und berechnet die Entropie. Dazu wird noise auf die Entropy addiert. Das Feld mit ner niedrigsten Entropy wird zurückgegeben
        for (int i = 0; i < wave.Length; i++)
        {
            //if(i == 28) Debugger.Break();

            if (!periodic && (i % MX + N > MX || i / MX + N > MY)) continue;

            if (extendedHeuristic == ExtendedHeuristic.LowestNodesFirst)
            {
                //if (remainingNormal > 1 && inputField[i].Equals("root")) continue;      //TESTING: if grass pieces arent solved, dont select root pieces
                //if (remainingNormal > 1 && inputField[i].Equals("root")) continue;      //TESTING: if grass pieces arent solved, dont select root pieces
                int nDepth = nodeDepth[inputField[i]];
                if(nDepth < remainingDepth) { continue; }
            }
            

            int remainingValues = sumsOfOnes[i];
            double entropy = heuristic == Heuristic.Entropy ? entropies[i] : remainingValues;

            if(entropy < 0)
            {

            }

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

        if(argmin >= 0) 
        {
            int x = argmin % MX;
            int y = argmin / MX; 
            //UnityEngine.Debug.Log($"Chose x:{x}, y: {y}");
        }
        else
        {
            UnityEngine.Debug.Log($"Chose {argmin}");
        }

        if(argmin == -1 && backtracking)
        {
            if (!isSolvable()) return -2;
        }

        return argmin;
    }

    int NextUnobservedNode2(System.Random random)
    {
        return 1;
    }

    void Observe(int node, System.Random random)       //node = Position im Feld
    {
        //bool[] w = wave[node];
        Dictionary<int, bool> w = wave[node];
        string nodeName = inputField[node];

        int x1 = node % MX;
        int y1 = node / MX;
        //UnityEngine.Debug.Log($"");

        /*
        for (int t = 0; t < ; t++)
        { 
            distribution[nodeName][t] = w[t] ? weights[t] : 0.0;      //Setzt die Verteilung aller noch möglichen Tiles in ein neues Array
            //evtl. falls es das Pattern in dem Cluster nicht gibt und dennoch drüberiteriert wird, einfach -1 sagen?
        }
        */

        foreach (var weight in weights[nodeName])
        {
            if (!distribution[nodeName].ContainsKey(weight.Key)) 
            {
                distribution[nodeName].Add(weight.Key, 0.0);
            }

            distribution[nodeName][weight.Key] = w[weight.Key] ? weight.Value : 0.0;
        }

        int r = distribution[nodeName].Random(random.NextDouble());                           //Nächstes Pattern welches gesetzt werrden soll

        //UnityEngine.Debug.Log($"Next Pos at {x1},{y1} | Chose ID: " + r);

        /*
        for (int t = 0; t < T[nodeName]; t++)
        {
            if (w[t] != (t == r))                                                   //Alle anderen Tiles außer dem auserwählten Tile werden gebannt, sodass nur noch das Auserwählte Tile übrig bleibt
            {
                Ban(node, t);
            }
        }
        */

        /*
        foreach(int patternID in clusterPatterns[nodeName])
        {
            if (w[patternID] != (patternID == r))
            {
                Ban(node, patternID);
            }
        }
        */

        for(int patternID = 0; patternID < globalPatternCount; patternID++)
        {
            if (!w.ContainsKey(patternID)) continue;

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

                
                int i2 = x2 + y2 * MX;              //2-dim position des Nachbarn wieder in 1-dim position umwandeln

                string neighbourNodeName = inputField[i2];
                int currentNodeDepth = nodeDepth[currentNodeName];
                int neighbourNodeDepth = nodeDepth[neighbourNodeName];
                //if (neighbourNodeName != currentNodeName) continue;                                 //Falls das benachbarte Feld zu einem anderen Cluster gehört, vorerst überspringen

                //current node name ist identisch mit dem nachbar, also kann ich dieses einfach weiter laufen lassen
                int[] p = propagator[neighbourNodeName][d][t1];        //holt sich alle möglichen Teile für diese Konstellation und das spezifische Tile heraus
                //int[] p = propagator[currentNodeName][d][t1];        //holt sich alle möglichen Teile für diese Konstellation und das spezifische Tile heraus


                //TESTING: if p == 0 skip, because pattern isn't available
                if (p == null)
                {
                    continue;
                }

                /*
                 * Problembeschreibung:
                 * Es werden nicht alle Patterns des benachbarten Teils gebannt, falls dieses vom anderen Clustertyp ist (aktuelle ist Graß, benachbart ist Root).
                 * Evtl dann immer den globalen oder den propagator des benachbarten Tiles nutzen. Letzteres macht mehr Sinn, jedoch sind die Ergebnisse falsch.
                 * Compatible sollte schonmal mit der richtigen Anzahl des benachbarten Tiles initialisiert werden, auch wenn dies von einem anderen Cluster ist.
                 */

                

                int[][] compat = compatible[i2];    

                for (int l = 0; l < p.Length; l++)
                {
                    if (banLowerClusterInRoot && currentNodeDepth < neighbourNodeDepth) continue;

                    if (i2 == 0)
                    {
                        //Debugger.Break();
                    }

                    int t2 = p[l];
                    int[] comp = compat[t2];
                    comp[d]--;
                    if (comp[d] == 0) Ban(i2, t2);
                }
            }

            if (!isSolvable() && initPhase)
            {
                UnityEngine.Debug.Log("Not Solvable");
            }
        }

        //ADDITION: Proper contradiction testing through all tiles.
        bool contradiction = false;

        for(int i = 0; i < sumsOfOnes.Length; i++)
        {
            if (sumsOfOnes[i] <= 0)
            {
                contradiction = true;
                int x = i % MX;
                int y = i / MX;
                UnityEngine.Debug.Log($"Contradiction at: x:{x}, y:{y}, i:{i}");
                break;
            }
        }

        //return sumsOfOnes[0] > 0;
        return !contradiction;
    }

    void Ban(int i, int t)      //i = Position im Feld, t = PatternID
    {
        int x1 = i % MX;
        int y1 = i / MX;
        //UnityEngine.Debug.Log($"Ban at {x1},{y1}");


        wave[i][t] = false;

        int contributors = 0;
        //----------TESTING---------
        foreach(var entry in wave[i])
        {
            if (entry.Value)
            {
                contributors++;
            }
        }

        if(contributors == 0)
        {
            //UnityEngine.Debug.Log("Contradiction in Banning");
        }

        //--------------------------

        int[] comp = compatible[i][t];

        for (int d = 0; d < 4; d++) comp[d] = 0;
        //stack[stacksize] = (i, t);                  //Für jedes Tile das gebannt wurde, wird dieses auf den Stack geschoben, da alle angrenzenden Tiles nun auch geprüft werden müssen
        //stacksize++;
        

        stack.Add((i, t));
        string nodeName = inputField[i];

        if (!clusterPatterns[nodeName].Contains(t)) return;     //TESTING

        sumsOfOnes[i] -= 1;
        sumsOfWeights[i] -= weights[nodeName][t];

        if (sumsOfOnes[i] < 0)
        {

        }

        sumsOfWeightLogWeights[i] -= weightLogWeights[nodeName][t];

        double sum = sumsOfWeights[i];

        entropies[i] = Math.Log(sum) - sumsOfWeightLogWeights[i] / sum;
    }

    void PreBanning()
    {
        for (int i = 0; i < wave.Length; i++)
        {
            int x1 = i % MX;
            int y1 = i / MX;

            //if(x1 + N > MX || y1 + N > MY) continue;

            string currentCluster = inputField[i];

            for (int d = 0; d < 4; d++)
            {
                int x2 = x1 + dx[d];
                int y2 = y1 + dy[d];
                if (!periodic && (x2 < 0 || y2 < 0 || x2 + N > MX || y2 + N > MY)) continue;        //Falls außerhalb der Boundary, entweder ignorieren (continue) oder wrapping

                

                int i2 = x2 + y2 * MX;              //2-dim position des Nachbarn wieder in 1-dim position umwandeln
                string neighbourCluster = inputField[i2];

                if (nodeDepth[neighbourCluster] <= nodeDepth[currentCluster]) continue;     //Es sollen nur von einem aktuellen Root Pixel ausgehen, dessen Nachbar kleiner bsp. Wasser ist um in diese zu limitieren
                //UnityEngine.Debug.Log($"PreBanning at:{x2}, {y2}");


                /* Ziel dieser Funktion:
                 * - Patterns die nicht kompatibel sein können komplett bannen
                 * - Entropy und Compatible Anzahl anpassen falls Nachbarn nicht kompatibel sein können ? Evtl durchs bannen schon beseitigt?
                 * 
                 */

                //Ermittle alle Patterns, die nicht kompatibel sein können zum niedrigeren Cluster nebenan
                //Falls es Patterns gibt, die nicht kompatibel sein können zum Nachbarn, diese bannen -> stack.Add(i, t)

                int[][] propagatorDirection = propagator[currentCluster][d];    //alle erlaubten patterns vom übergeordneten Cluster in die Himmelsrichtung

                HashSet<int> allowedPatterns = new();
                HashSet<int> toBannedPatterns = new HashSet<int>(clusterPatterns[currentCluster]);


                /* Prüfe alle Patterns ab, die in die angegebene Richtung erlaubte Patterns haben
                 * bspw. alle PatternIDs abspeichern, die nach rechts mind. ein erlaubtes Pattern aus Wassercluster besitzt
                 * 
                 */
                for(int t = 0; t < globalPatternCount; t++)
                {
                    foreach(int patternID in propagatorDirection[t])
                    {
                        if (clusterPatterns[neighbourCluster].Contains(patternID))
                        {
                            allowedPatterns.Add(t);
                        }
                    }
                }

                //Im Umkehrschluss müssen nun alle nicht erlaubten Patterns gebannt werden
                /* Nicht eigenhändig neu berechnet werden müssen: entropy, sumOfOnes, sumsOfWeights, sumsOfWeightLogWeights und vor ALLEM compatible
                 * wave wird automatisch bei der neuen Ban Funktion geändert, genauso dann entropy und die sums-variablen
                 */

                toBannedPatterns.ExceptWith(allowedPatterns);

                //evtl auch einfach alle dem Stack hinzufügen und dann die vordefinierte Ban Funktion nutzen? Diese Testen

                foreach(int patternID in toBannedPatterns)
                {
                    if(!stack.Contains((i, patternID)))
                    {
                        //stack.Add((i, patternID));
                        Ban(i, patternID);
                    }
                }

                //PreBan erstmal noch nicht nutzen. Erst die variante ausprobieren.
                //PreBan(i2, t, opposite[d]);      //d -> Die Richtung, in der die Teile gebannt werden müssen

            }
        }

        Propagate();       //Stack der durchs PreBanning angefallen ist, muss abgearbeitet werden
    }

    void Clear()
    {
        modelStates = new();
        for (int i = 0; i < wave.Length; i++)
        {
            string nodeName = inputField[i];
            int currentNodeDepth = nodeDepth[nodeName];
            decided[i] = false;
            /*
            for (int t = 0; t < T[nodeName]; t++)
            {
                wave[i][t] = true;
                for (int d = 0; d < 4; d++) compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;    //Speichere die Menge an noch kompatiblen Tiles in diese Himmelsrichtung ab
            }
            */

            wave[i] = new();
            int x1 = i % MX;
            int y1 = i / MX;

            //ADDITION
            foreach (var t in clusterPatterns[nodeName])
            //foreach (var t in clusterPatterns["root"])
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
                    if (compatibleInit == CompatibleInit.Original)
                    {
                        /*
                        int x2 = x1 + dx[d];
                        int y2 = y1 + dy[d];
                        if (!periodic && (x2 < 0 || y2 < 0 || x2 + N > MX || y2 + N > MY))
                        {
                            continue;
                        }
                        */

                        compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;      //Speichere die Menge an noch kompatiblen Tiles in diese Himmelsrichtung ab
                        //compatible[i][t][d] = propagator["root"][opposite[d]][t].Length;      //Speichere die Menge an noch kompatiblen Tiles in diese Himmelsrichtung ab
                    }
                    else if(compatibleInit == CompatibleInit.New)
                    {

                        //------New variant of compatible array init------------

                        /* Zwei Probleme bei compatible:
                         * - Es muss auch über das Feld hinaus gefüllt werden. Gründe bisher unklar. 
                         * - Es muss richtig gefüllt werden je nachdem welches Cluster am Rand angrenzt
                         * Evtl Problem in Propagate Funktion suchen
                         */

                        int x2 = x1 + dx[d];
                        int y2 = y1 + dy[d];
                        compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;

                        /*
                        if (!periodic && (x2 < 0 || y2 < 0 || x2 + N > MX || y2 + N > MY))
                        {
                            //compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length; 
                            //compatible[i][t][d] = propagator["root"][opposite[d]][t].Length;
                            //compatible[i][t][d] = 50;
                            continue;
                        }
                        */

                        int i2 = x2 + y2 * MX;              //2-dim position des Nachbarn wieder in 1-dim position umwandeln

                        if (i2 < 0 || i2 > inputField.Length - 1) continue;

                        string neighbourNodeName = inputField[i2];
                        int neighbourNodeDepth = nodeDepth[neighbourNodeName];


                        /*
                         * 3 cases:
                         * - Benachbarte Pixel sind im selben Cluster: Gleich bleibend wie im orig. WFC
                         * - Aktuelle Pixel (root) ist niedrigere Depth als der Nachbar (water): Zählen wie viele kompatible Pattern es von root zu water Tiles gibt und diese Anzahl speichern
                         * - Aktuelle Pixel (water) ist höhere Depth als der Nachbar (root): Zählen wie viele kompatible Pattern es aus Water gibt, die es zu root Teilen gibt und diese Abspeichern.
                         *      Ansonsten erstmal standard Handhabung wie im orig. WFC und bei Problemen dies mal probieren
                         */

                        if(nodeName == neighbourNodeName)
                        {
                            //compatible[i][t][d] = propagator["root"][opposite[d]][t].Length;
                            compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;
                        }
                        else if(neighbourNodeDepth > currentNodeDepth)
                        {
                            //nachbar water, current root
                            compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;
                        }
                        else if(neighbourNodeDepth < currentNodeDepth)
                        {
                            try
                            {
                                compatible[i][t][d] = propagator[neighbourNodeName][opposite[d]][t].Length;
                            }
                            //nachbar root, current water
                            catch (Exception e)
                            {
                                compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;
                            }

                            /*
                            var p = propagator[nodeName];
                            var p2 = p[opposite[d]][t];
                            HashSet<int> patternComp = new HashSet<int>();
                            foreach (int pattID in clusterPatterns[neighbourNodeName])
                            {
                                if (p2.Contains(pattID)) patternComp.Add(pattID);
                            }
                            compatible[i][t][d] = patternComp.Count;
                            */
                        }
                        else
                        {
                            //Problem: Komischerweise ist manchmal wasser nachbar von grass und umgekehrt. Komisch
                            compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;
                        }
                    }
                    else
                    {

                        int x2 = x1 + dx[d];
                        int y2 = y1 + dy[d];

                        if (!periodic && (x2 < 0 || y2 < 0 || x2 + N > MX || y2 + N > MY))
                        {
                            compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;
                            //compatible[i][t][d] = propagator["root"][opposite[d]][t].Length;
                            //compatible[i][t][d] = -1;
                            continue;
                        }

                        int i2 = x2 + y2 * MX;              //2-dim position des Nachbarn wieder in 1-dim position umwandeln
                        string neighbourNodeName = inputField[i2];
                        int neighbourNodeDepth = nodeDepth[neighbourNodeName];

                        if (neighbourNodeDepth == currentNodeDepth)
                        {
                            compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;
                        }
                        else if (neighbourNodeDepth > currentNodeDepth)
                        {
                            //nachbar water, current root
                            compatible[i][t][d] = propagator[nodeName][opposite[d]][t].Length;
                        }
                        else
                        {
                            var p = propagator[nodeName];
                            var p2 = p[opposite[d]][t];
                            HashSet<int> patternComp = new HashSet<int>();
                            foreach (int pattID in clusterPatterns[neighbourNodeName])
                            {
                                if (p2.Contains(pattID)) patternComp.Add(pattID);
                            }
                            compatible[i][t][d] = patternComp.Count;

                        }

                        /*
                        int x2 = x1 + dx[d];
                        int y2 = y1 + dy[d];
                        //if (!periodic && (x2 < 0 || y2 < 0 || x2 + N > MX || y2 + N > MY)) continue;        //Falls außerhalb der Boundary, entweder ignorieren (continue) oder wrapping

                        if (x2 < 0) x2 += MX;
                        else if (x2 >= MX) x2 -= MX;
                        if (y2 < 0) y2 += MY;
                        else if (y2 >= MY) y2 -= MY;

                        int i2 = x2 + y2 * MX;              //2-dim position des Nachbarn wieder in 1-dim position umwandeln
                        string neighbourNodeName = inputField[i2];

                        var p = propagator[neighbourNodeName];
                        var p2 = p[opposite[d]];
                        var p3 = p2[t];
                        //Speichere die Menge an noch kompatiblen Tiles in diese Himmelsrichtung ab
                        if (p3 == null)
                        {
                            compatible[i][t][d] = 0;    //Existiert kein kompatibles Pattern, so setze es auf 0
                        }
                        else
                        {
                            compatible[i][t][d] = p3.Length;
                        }
                        */

                    }
                    
                }
                
            }


            sumsOfOnes[i] = T[nodeName];
            sumsOfWeights[i] = sumOfWeights[nodeName];
            sumsOfWeightLogWeights[i] = sumOfWeightLogWeights[nodeName];
            entropies[i] = startingEntropy[nodeName];
            observed[i] = -1;
        }
        observedSoFar = 0;
        backtrackTriesCounter = 0;

        preFinishedObserved = null;
        preFinishedWave = null;

        int maxValue = 0;
        foreach(var depth in nodeDepth)
        {
            if(depth.Value > maxValue) maxValue = depth.Value;
        }

        remaining.Clear();
        for(int i = 0; i <= maxValue; i++)
        {
            remaining.Add(0);
        }

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

    void CreateBacktrackStep(List<ModelState> ms = null)
    {
        if (ms == null) ms = modelStates;

        ModelState modelState = new ModelState();
        modelState.wave = new Dictionary<int, bool>[wave.Length];
        for(int i = 0; i < wave.Length; i++)
        {
            modelState.wave[i] = new Dictionary<int, bool>(wave[i]);
        }

        //modelState.compatible = compatible.Clone() as int[][][];
        modelState.compatible = new int[compatible.Length][][];
        for(int i = 0; i < compatible.Length; i++)      //Deep copy of jagged array
        {
            modelState.compatible[i] = new int[compatible[i].Length][];
            for(int j = 0; j < compatible[i].Length; j++)
            {
                //modelState.compatible[i][j] = compatible[i][j].Clone() as int[];
                modelState.compatible[i][j] = compatible[i][j].Select(a => a).ToArray();
            }
        }
        modelState.observed = observed.Clone() as int[];
        modelState.sumOfWeights = new Dictionary<string, double>(sumOfWeights);
        modelState.sumOfWeightLogWeights = new Dictionary<string, double>(sumOfWeightLogWeights);
        modelState.sumsOfWeights = sumsOfWeights.Select(a => a).ToArray();
        modelState.sumsOfWeightLogWeights = sumsOfWeightLogWeights.Select(a => a).ToArray();
        //modelState.entropies = entropies.Clone() as double[];
        modelState.entropies = entropies.Select(a => a).ToArray();
        //modelState.sumsOfOnes = sumsOfOnes.Clone() as int[];
        modelState.sumsOfOnes = sumsOfOnes.Select(a => a).ToArray();

        double lowest = 1E4;
        int amount = 0;
        foreach(double entropy in entropies)
        {
            if (entropy < lowest)
            {
                lowest = entropy;
                amount = 0;
            }

            if (entropy.AlmostEqualTo(lowest))
            {
                amount++;
            }
        }

        modelState.lowestEntropyCount = amount;
        modelState.lowestEntropy = lowest;

        ms.Add(modelState);
    }

    void BacktrackRestore(int steps)
    {
        if(steps == -1)
        {
            //If -1, get the last state from the list with more than one possibilities to select
            if(modelStates.Count > 1)
            {
                var lastElement = from s in modelStates where !s.lowestEntropy.AlmostEqualTo(0.0) && s.lowestEntropyCount > 0 select s;
                if(lastElement.Count() == 0)
                {
                    //steps = modelStates.Count - 1;  //Geh ganz zurück an den Anfang
                    steps = 10;
                }
                else 
                {
                    ModelState last = lastElement.Last();
                    steps = modelStates.Count - modelStates.IndexOf(last);
                }
            }
            else steps = 1;

            if(backtrackTriesCounter % 50 == 0)
            {
                steps = modelStates.Count - 1;
            }
        }

        for(int i = 0; i < steps; i++)
        {
            if(modelStates.Count <= 1) break;
            modelStates.RemoveAt(modelStates.Count - 1);
        }

        ModelState restoreState = modelStates[modelStates.Count - 1];

        wave = new Dictionary<int, bool>[restoreState.wave.Length];
        for(int i = 0; i < restoreState.wave.Length; i++)
        {
            wave[i] = new Dictionary<int, bool>(restoreState.wave[i]);
        }

        compatible = new int[restoreState.compatible.Length][][];
        for(int i = 0; i < restoreState.compatible.Length; i++)
        {
            compatible[i] = new int[restoreState.compatible[i].Length][];
            for(int j = 0; j < restoreState.compatible[i].Length; j++)
            {
                compatible[i][j] = restoreState.compatible[i][j].Select(a => a).ToArray();
            }
        }

        observed = restoreState.observed.Clone() as int[];
        sumOfWeights = new Dictionary<string, double>(restoreState.sumOfWeights);
        sumOfWeightLogWeights = new Dictionary<string, double>(restoreState.sumOfWeightLogWeights);
        sumsOfWeights = restoreState.sumsOfWeights.Select(a => a).ToArray();
        sumsOfWeightLogWeights = restoreState.sumsOfWeightLogWeights.Select(a => a).ToArray();
        entropies = restoreState.entropies.Select(a => a).ToArray();
        sumsOfOnes = restoreState.sumsOfOnes.Select(a => a).ToArray();
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

    private bool isSolvable()
    {
        for (int i = 0; i < inputField.Length; i++)
        {
            //if (!(i % MX + N > MX || i / MX + N > MY)) continue;
            int x1 = i % MX;
            int y1 = i / MX;
            int trueAmount = 0;

            foreach (var l in wave[i])
            {
                if (l.Value) trueAmount++;
            }

            if (trueAmount == 0)
            {
                //UnityEngine.Debug.Log($"Not Solvable at x:{x1}, y:{y1}, i{i}");
                return false;      //If an undecidable piece exist, return false
            }
        }

        return true;
    }

    private void SaveCurrentProgress()
    {
        preFinishedObserved = new int[wave.Length];
        preFinishedWave = new Dictionary<int, bool>[wave.Length];
        preDecided = decided.Clone() as bool[];
        for (int i = 0; i < wave.Length; i++)
        {
            string nodeName = inputField[i];

            //if some tiles are already set, set the patternID for the tile for generating a snapshot picture of the current generation progress
            if (decided[i])
            {
                foreach (int patternID in clusterPatterns[nodeName])
                {
                    if (wave[i][patternID])
                    {
                        preFinishedObserved[i] = patternID;
                        break;
                    }
                }
            }
            preFinishedObserved[0] = -1;    //Erkennung für Generation
            preFinishedWave[i] = new Dictionary<int, bool>(wave[i]);
        }
    }

    private void calculateRemaining()
    {
        for(int i = 0; i < remaining.Count; i++)
        {
            remaining[i] = 0;
        }

        for(int i = 0; i < wave.Length; i++)
        {
            string nodeName = inputField[i];
            int trueAmount = 0;

            foreach (var l in wave[i])
            {
                if (!(i % MX + N > MX || i / MX + N > MY))
                    if (l.Value) trueAmount++;
            }

            if (trueAmount > 1)
            {
                int nDepth = nodeDepth[nodeName];
                //UnityEngine.Debug.Log(nDepth);
                remaining[nDepth]++;
                //decided[i] = true;
            }
            else if (trueAmount == 1)
            {
                decided[i] = true;
            }
            else
            {
                decided[i] = false;
            }
        }
    }

    private int getNextUndecidedNodeDepth()
    {
        for (int i = remaining.Count - 1; i >= 0; i--)
        {
            if (remaining[i] != 0) return i;
        }

        return -1;
    }

    private double[] SaveEntropies()
    {
        double[] entropySave = new double[wave.Length];

        for(int i = 0; i < wave.Length; i++)
        {
            entropySave[i] = entropies[i];
        }

        return entropySave;
    }

    private Dictionary<int, bool>[] SaveWave()
    {
        Dictionary<int, bool>[] wSave = new Dictionary<int, bool>[wave.Length];

        for (int i = 0; i < wave.Length; i++)
        {
            wSave[i] = new Dictionary<int, bool>(wave[i]);
        }

        return wSave;
    }

    private void calcRemainingSum()
    {
        remainingPatternsSum = new int[wave.Length];

        for(int i = 0; i < wave.Length; i++)
        {
            int contributors = 0;

            foreach (var entry in wave[i])
            {
                if (entry.Value)
                {
                    contributors++;
                }
            }

            remainingPatternsSum[i] = contributors;
        }
    }

    public abstract void Save(string filename);

    //implementierung um die Tiles eines Tile herum zu schauen (links, unten, rechts, oben)
    protected static int[] dx = { -1, 0, 1, 0 };
    protected static int[] dy = { 0, 1, 0, -1 };
    static int[] opposite = { 2, 3, 0, 1 };     //was ist die entgegenliegende Tile position
}
