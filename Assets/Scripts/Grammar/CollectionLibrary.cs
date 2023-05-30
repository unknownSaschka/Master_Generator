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

	public string Selected = "";
	[HideInInspector] public int SelectedFolder = 0;

	private string CollectionsFolder = "Assets/Prefabs/Collections";



    protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName.Equals("Input")) return GetInputValue<GrammarNode>("Input", Input);
		else return null;
	}

	public List<string> GetFolder()
	{
		var folders = AssetDatabase.GetSubFolders(CollectionsFolder);
		List<string> result = new List<string>();
		foreach (var folder in folders)
		{
			result.Add(folder.Substring(folder.LastIndexOf('/') + 1));
		}

		return result;
	}

	public string GetSelectedFolder()
	{
		return CollectionsFolder + "/" + Selected;
	}
}