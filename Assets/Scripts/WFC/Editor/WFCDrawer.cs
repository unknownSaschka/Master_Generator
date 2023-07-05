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


        if(wfc.StructureBitmapTexture == null ) 
        {
            if (GUILayout.Button("Load Structure"))
            {
                wfc.LoadStructure();
            }
        }
        else
        {
            if (GUILayout.Button(wfc.StructureBitmapTexture.ToTexture2D(), GUILayout.MinWidth(100), GUILayout.MaxWidth(500), GUILayout.MinHeight(100), GUILayout.MaxHeight(200)))
            {
                wfc.LoadStructure();
            }
        }

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

        if (GUILayout.Button("Load Graph"))
        {
            string newJSONPath = EditorUtility.OpenFilePanel("Select Graph JSON", "", "json");
            if(newJSONPath.Length > 0)
            {
                wfc.GraphPath = newJSONPath;
            }

            wfc.LoadJSON();
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
    }

    
}
