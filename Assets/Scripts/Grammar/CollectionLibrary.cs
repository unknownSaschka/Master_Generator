using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNode;

public class CollectionLibrary : GrammarNode
{
	// Auswahl der Ordner per Helpboxes
	[Input] public GrammarNode Input;
	[Output] public GrammarNode Next;

    
    protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}

	public void LoadFolder()
	{
		var folders = AssetDatabase.GetSubFolders("Assets/Prefabs/Collections");
		foreach (var folder in folders)
		{
			Debug.Log(folder);
		}
	}
}