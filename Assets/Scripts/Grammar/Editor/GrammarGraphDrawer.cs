using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GrammarGraph))]
public class GrammarGraphDrawer : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        GrammarGraph graph = (GrammarGraph)target;

        if (GUILayout.Button("Text"))
        {
            graph.GetLevel();
        }
    }
}
