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


        if(wfc.BitmapTexture == null ) 
        {
            if (GUILayout.Button("Load Image"))
            {
                LoadImage(wfc);
            }
        }
        else
        {
            if (GUILayout.Button(wfc.BitmapTexture, GUILayout.MinWidth(100), GUILayout.MaxWidth(500), GUILayout.MinHeight(100), GUILayout.MaxHeight(200)))
            {
                LoadImage(wfc);
            }
        }

        if(GUILayout.Button("Load Graph"))
        {
            string newJSONPath = EditorUtility.OpenFilePanel("Select Graph JSON", "", "json");
            if(newJSONPath.Length > 0)
            {
                wfc.GraphPath = newJSONPath;
            }

            wfc.Nodes = JSONParser.ParseJSON(wfc.GraphPath);
        }
    }

    private void LoadImage(WFC wfc)
    {
        wfc.BitmapPath = EditorUtility.OpenFilePanel("Select Bitmap", "", "bmp");
        wfc.BitmapTexture = InputImage.LoadImage(wfc.BitmapPath);
    }
}
