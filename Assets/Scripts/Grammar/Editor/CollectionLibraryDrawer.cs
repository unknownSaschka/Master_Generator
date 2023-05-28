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

    private int _selectedFolder = 0;

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
            
        }

        var folders = collectionLibrary.GetFolder();

        _selectedFolder = EditorGUILayout.Popup(_selectedFolder, folders.ToArray());
        collectionLibrary.Selected = folders[_selectedFolder];
    }
}
