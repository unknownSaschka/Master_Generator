using B83.Image.BMP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class WFC : MonoBehaviour
{
    System.Random random = new();

    public string ImageOutputFolder;

    public string StructureBitmapPath;
    public Texture2D PrototypeBitmapTexture;

    public string GraphPath;
    public Dictionary<string, Node> Nodes;
    public string RootNodeName;

    public Texture2D SampleBitmap;
    public string SampleBitmapPath;

    public GameObject PrototypePlane;
    public GameObject SamplePlane;
    public GameObject ResultPlane;

    private Texture2D Result;

    //TODO Sample List in Node or smth
    public int Limit;
    public int SampleSize;
    public int Width, Height;
    public bool PeriodicInput;
    public bool Periodic;
    public int Symmetry;
    public bool Ground;
    public Helper.Heuristic Heuristic;
    public Helper.ExtendedHeuristic ExtendedHeuristic;
    public Helper.CompatibleInit CompatibleInit;

    private OverlappingModel currentModel;

    private ClusterOverlapping clusterModel;

    public int MilliSecondsWait = 10;
    public int Retries = 20;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    

    public void LoadSample()
    {
        SampleBitmapPath = EditorUtility.OpenFilePanel("Select Sample Bitmap", "", "png");
        SampleBitmap = InputImage.LoadPNG(SampleBitmapPath);
        SetImageOnObject(SamplePlane, SampleBitmap);
    }

    public void PrepareModel()
    {
        currentModel = new OverlappingModel(SampleBitmap.GetBitmap(), SampleBitmap.width, SampleBitmap.height, SampleSize, Width, Height, PeriodicInput, Periodic, Symmetry, Ground, Heuristic);
    }

    public void GeneratePicture()
    {
        OverlappingModel model = new OverlappingModel(SampleBitmap.GetBitmap(), SampleBitmap.width, SampleBitmap.height, SampleSize, Width, Height, PeriodicInput, Periodic, Symmetry, Ground, Heuristic);
        for(int i = 0; i < 10; i++)
        {
            bool success = model.Run(random.Next(), -1) ;
            if (success) break;
        }
        int[] image = model.GenerateBitmap();
        SetImageOnObject(ResultPlane, GetTextureFromInt(image));
    }

    public void SteppedGenerate()
    {
        if (currentModel == null) PrepareModel();
        currentModel.InitStepRun();

        //StartCoroutine(StepGenerate());
        StepGenerate();
    }

    private async void StepGenerate()
    {
        int skip = 5;
        int index = 0;

        for(int tries = 0; tries < Retries; tries++)
        {
            bool contradiction = false;

            for (int i = 0; i < 50000; i++)
            {
                int finished = currentModel.StepRun(random);
                index++;

                //Debug.Log(finished);
                if (finished == -1)     //CONTRADICTION, Fehler beim Generieren
                {
                    //yield break;
                    Debug.Log("CONTRADICTION");
                    contradiction = true;
                    break;
                }

                if (finished == 0)       //Fertig
                {
                    int[] current = currentModel.GenerateBitmap();
                    SetImageOnObject(ResultPlane, GetTextureFromInt(current));
                    //yield return new WaitForSecondsRealtime(SecondsWait);
                    return;
                }

                if (finished == 1)       //Aktiv
                {
                    if (index >= skip)
                    {
                        index = 0;
                        int[] current = currentModel.GenerateBitmap();
                        SetImageOnObject(ResultPlane, GetTextureFromInt(current));
                        //yield return new WaitForSeconds(SecondsWait);
                        await Task.Delay(MilliSecondsWait);
                    }
                }

                //int[] current = currentModel.GenerateBitmap();
                //SetImageOnObject(ResultPlane, GetTextureFromInt(current));
                //yield return new WaitForSecondsRealtime(SecondsWait);
            }

            if (!contradiction)
            {
                int[] result = currentModel.GenerateBitmap();
                SetImageOnObject(ResultPlane, GetTextureFromInt(result));
                return;
            }
            else
            {
                currentModel.InitStepRun();
            }
        }
    }

    //------------------ NEW VARIANT -------------------------------
    public void LoadJSON()
    {
        Nodes = JSONParser.ParseJSON(GraphPath);
    }

    public void LoadStructure()
    {
        StructureBitmapPath = EditorUtility.OpenFilePanel("Select Bitmap", "", "bmp");
        PrototypeBitmapTexture = InputImage.LoadImage(StructureBitmapPath).ToTexture2D();
        SetImageOnObject(PrototypePlane, PrototypeBitmapTexture);
        Width = PrototypeBitmapTexture.width; 
        Height = PrototypeBitmapTexture.height;
    }

    public void PrepareClusteredOverlapping()
    {
        Dictionary<string, Node> leafs = new();

        foreach(var node in Nodes)
        {
            //if(node.Value.Sample == null) continue;
            leafs.Add(node.Key, node.Value);
        }

        clusterModel = new ClusterOverlapping(leafs, PrototypeBitmapTexture, SampleSize, PrototypeBitmapTexture.width, PrototypeBitmapTexture.height, Periodic, Ground, Heuristic, ExtendedHeuristic, CompatibleInit);
    }

    public void GenerateClusteredOverlapping()
    {
        /*
        for(int i = 0; i < 100; i++)
        {
            //bool success = clusterModel.Run(random.Next(), Limit);
            bool success = clusterModel.Run(random, Limit);
            if (success) break;
        }
        */
        clusterModel.Run(random, Limit);

        int[] image = clusterModel.GenerateBitmap();
        Texture2D result = GetTextureFromInt(image);
        PrototypeParser p = new PrototypeParser();
        result = p.CutTexture(result, SampleSize);
        SetImageOnObject(ResultPlane, result);
    }

    public async void ClusteredStepGenerate()
    {
        clusterModel.InitStepRun();

        foreach (var finish in clusterModel.StepRun(random, Limit))
        {
            int[] image = clusterModel.GenerateBitmap();
            Texture2D result = GetTextureFromInt(image);
            PrototypeParser p = new PrototypeParser();
            result = p.CutTexture(result, SampleSize);
            SetImageOnObject(ResultPlane, result);
            await Task.Delay(MilliSecondsWait);

            if (finish) break;
        }
    }

    public void SetImageOnObject(GameObject gameObject, BMPImage texture)
    {
        Texture2D img = texture.ToTexture2D();
        img.filterMode = FilterMode.Point;
        var sampleRenderer = gameObject.GetComponent<Renderer>();
        sampleRenderer.sharedMaterial.SetTexture("_MainTex", img);
    }

    public void SetImageOnObject(GameObject gameObject, Texture2D texture)
    {
        var sampleRenderer = gameObject.GetComponent<Renderer>();
        sampleRenderer.sharedMaterial.SetTexture("_MainTex", texture);
    }

    private Texture2D GetTextureFromInt(int[] bitmap)
    {
        Color32[] colors = new Color32[bitmap.Length];
        for (int i = 0; i < bitmap.Length; i++)
        {
            byte[] cols = BitConverter.GetBytes(bitmap[i]);
            colors[i] = new Color32(cols[2], cols[1], cols[0], cols[3]);        //WFC saves in BGRA
        }

        Texture2D resultTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        resultTexture.SetPixels32(colors.FlipVertically(Width, Height));
        resultTexture.filterMode = FilterMode.Point;
        resultTexture.Apply();
        Result = resultTexture;

        return resultTexture;
    }

    public void ProcessPrototype()
    {
        PrototypeParser p = new PrototypeParser();
        Graph graph = new Graph(Nodes);
        Texture2D newPrototype = p.ProcessPrototype(PrototypeBitmapTexture, graph, RootNodeName);
        newPrototype = p.ExpandTexture(newPrototype, SampleSize);
        SetImageOnObject(SamplePlane, newPrototype);

        //var texturePNG = newPrototype.EncodeToPNG();
        //File.WriteAllBytes($"{ImageOutputFolder}PrototypeProcessed_{random.Next()}.png", texturePNG);
        Width = newPrototype.width;
        Height = newPrototype.height;

        PrototypeBitmapTexture = newPrototype;
    }

    public void SaveResult()
    {
        var texturePNG = Result.EncodeToPNG();
        File.WriteAllBytes($"{ImageOutputFolder}Sample N={SampleSize}_{random.Next()}.png", texturePNG);
    }
}
