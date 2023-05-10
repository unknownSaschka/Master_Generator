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

        if (GUILayout.Button("Generate"))
        {
            logic.Generate();
        }
    }
}
