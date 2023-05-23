using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

public class CollectionNode : GrammarNode
{

	// Use this for initialization
	protected override void Init() {
		base.Init();
		
		//load all folders which has collections in it
		//load all collections that can be selected in those folders
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}
}