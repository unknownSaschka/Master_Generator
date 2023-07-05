using B83.Image.BMP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class WFC : MonoBehaviour
{
    System.Random random = new();

    public string StructureBitmapPath;
    public BMPImage StructureBitmapTexture;

    public string GraphPath;
    public Dictionary<string, Node> Nodes;

    public Texture2D SampleBitmap;
    public string SampleBitmapPath;

    public GameObject StructurePlane;
    public GameObject SamplePlane;
    public GameObject ResultPlane;

    //TODO Sample List in Node or smth
    public int SampleSize;
    public int Width, Height;
    public bool PeriodicInput;
    public bool Periodic;
    public int Symmetry;
    public bool Ground;
    public Model.Heuristic Heuristic;

    private OverlappingModel currentModel;

    public int SecondsWait = 10;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadJSON()
    {
        Nodes = JSONParser.ParseJSON(StructureBitmapPath);
    }

    public void LoadStructure()
    {
        StructureBitmapPath = EditorUtility.OpenFilePanel("Select Bitmap", "", "bmp");
        StructureBitmapTexture = InputImage.LoadImage(StructureBitmapPath);
        SetImageOnObject(StructurePlane, StructureBitmapTexture);
    }

    public void LoadSample()
    {
        SampleBitmapPath = EditorUtility.OpenFilePanel("Select Sample Bitmap", "", "png");
        SampleBitmap = InputImage.LoadPNG(SampleBitmapPath);
        SetImageOnObject(SamplePlane, SampleBitmap);
    }

    public void PrepareModel()
    {
        currentModel = new OverlappingModel(FromTexture(SampleBitmap), SampleBitmap.width, SampleBitmap.height, SampleSize, Width, Height, PeriodicInput, Periodic, Symmetry, Ground, Heuristic);
    }

    public void GeneratePicture()
    {
        OverlappingModel model = new OverlappingModel(FromTexture(SampleBitmap), SampleBitmap.width, SampleBitmap.height, SampleSize, Width, Height, PeriodicInput, Periodic, Symmetry, Ground, Heuristic);
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

        for (int i = 0; i < 10000; i++)
        {
            int finished = currentModel.StepRun(random);
            index++;

            //Debug.Log(finished);
            if (finished == -1)     //CONTRADICTION, Fehler beim Generieren
            {
                //yield break;
                return;
            }

            if(finished == 0)       //Fertig
            {
                int[] current = currentModel.GenerateBitmap();
                SetImageOnObject(ResultPlane, GetTextureFromInt(current));
                //yield return new WaitForSecondsRealtime(SecondsWait);
                return;
            }

            if(finished == 1)       //Aktiv
            {
                if(index >= skip)
                {
                    index = 0;
                    int[] current = currentModel.GenerateBitmap();
                    SetImageOnObject(ResultPlane, GetTextureFromInt(current));
                    //yield return new WaitForSeconds(SecondsWait);
                    await Task.Delay(SecondsWait);
                }
            }

            //int[] current = currentModel.GenerateBitmap();
            //SetImageOnObject(ResultPlane, GetTextureFromInt(current));
            //yield return new WaitForSecondsRealtime(SecondsWait);
        }

        int[] result = currentModel.GenerateBitmap();
        SetImageOnObject(ResultPlane, GetTextureFromInt(result));
        //yield return result;
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

    private int[] FromTexture(Texture2D texture)
    {
        Color32[] imageData = texture.GetPixels32();

        //Converts Color32 Array to an int array with bgra32
        int[] img = new int[texture.width * texture.height];
        for (int i = 0; i < imageData.Length; i++)
        {
            byte[] bytes = new byte[4];
            bytes[0] = imageData[i].b;
            bytes[1] = imageData[i].g;
            bytes[2] = imageData[i].r;
            bytes[3] = imageData[i].a;
            img[i] = BitConverter.ToInt32(bytes, 0);
        }

        return img;
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
        resultTexture.SetPixels32(colors);
        resultTexture.filterMode = FilterMode.Point;
        resultTexture.Apply();

        return resultTexture;
    }
}
