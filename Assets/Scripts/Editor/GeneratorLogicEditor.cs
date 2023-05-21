using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GeneratorLogic))]
public class GeneratorLogicEditor : Editor
{
    public override void OnInspectorGUI()
    {

        base.OnInspectorGUI();
        GeneratorLogic logic = (GeneratorLogic)target;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-"))
        {
            logic.SimulationSteps--;
        }

        if(GUILayout.Button("+")) 
        {
            logic.SimulationSteps++;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("DeleteAll"))
        {
            logic.ClearAll();
        }

        if (GUILayout.Button("Delete Assets"))
        {
            logic.ClearPrefabs();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Prepare Collections"))
        {
            logic.PrepareCollections();
        }

        if (GUILayout.Button("Simulate Steps"))
        {
            logic.SimulateSteps();
        }

        GUILayout.Space(10);

        if(GUILayout.Button("Complete New Generation"))
        {
            logic.CompleteGeneration();
        }
    }
}
