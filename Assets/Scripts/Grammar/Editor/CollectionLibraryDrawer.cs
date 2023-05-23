using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNodeEditor;
using static XNodeEditor.NodeEditor;

[CustomNodeEditor(typeof(CollectionLibrary))]
public class CollectionLibraryDrawer : NodeEditor
{

    private CollectionLibrary collectionLibrary;

    public override void OnBodyGUI()
    {
        if(collectionLibrary == null)
        {
            collectionLibrary = target as CollectionLibrary;
        }

        base.OnBodyGUI();

        EditorGUILayout.Space(10);

        serializedObject.Update();

        if (GUILayout.Button("Load"))
        {
            collectionLibrary.LoadFolder();
        }
    }
}
