using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WFC))]
public class WFCDrawer : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        WFC wfc = (WFC)target;

        if (wfc.SampleBitmap == null)
        {
            if (GUILayout.Button("Load Sample"))
            {
                wfc.LoadSample();
            }
        }
        else
        {
            if (GUILayout.Button(wfc.SampleBitmap, GUILayout.MinWidth(100), GUILayout.MaxWidth(500), GUILayout.MinHeight(100), GUILayout.MaxHeight(200)))
            {
                wfc.LoadSample();
            }
        }
        

        if (GUILayout.Button("Generate"))
        {
            wfc.GeneratePicture();
        }

        if(GUILayout.Button("Prepare Model"))
        {
            wfc.PrepareModel();
        }

        if (GUILayout.Button("Stepped Generate"))
        {
            wfc.SteppedGenerate();
        }

        GUILayout.Label("New Variant");

        if (GUILayout.Button("Load Graph"))
        {
            string newJSONPath = EditorUtility.OpenFilePanel("Select Graph JSON", "", "json");
            if (newJSONPath.Length > 0)
            {
                wfc.GraphPath = newJSONPath;
            }

            wfc.LoadJSON();
        }

        if (wfc.PrototypeBitmapTexture == null)
        {
            if (GUILayout.Button("Load Structure"))
            {
                wfc.LoadStructure();
            }
        }
        else
        {
            if (GUILayout.Button(wfc.PrototypeBitmapTexture, GUILayout.MinWidth(100), GUILayout.MaxWidth(500), GUILayout.MinHeight(100), GUILayout.MaxHeight(200)))
            {
                wfc.LoadStructure();
            }
        }

        if (GUILayout.Button("Process Prototype"))
        {
            wfc.ProcessPrototype();
        }

        GUILayout.Label("Normal Generation");

        if (GUILayout.Button("Prepare Clustered"))
        {
            wfc.PrepareClusteredOverlapping();
        }

        if(GUILayout.Button("Generate Clustered"))
        {
            wfc.GenerateClusteredOverlapping();
        }

        GUILayout.Label("Step by Step Generation");

        if (GUILayout.Button("Init Step Generation"))
        {
            wfc.InitClusteredStepGeneration();
        }

        if (GUILayout.Button("Step Generate Clustered"))
        {
            wfc.ClusteredStepGenerate();
        }

        GUILayout.Label("Save Result");

        if (GUILayout.Button("Save Result"))
        {
            wfc.SaveResult();
        }
    }

    
}
