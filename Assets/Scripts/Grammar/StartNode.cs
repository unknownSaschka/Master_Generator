using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[NodeTint("#49c9a0")]
[NodeWidth(200)]
public class StartNode : GrammarNode
{

	[Output] public GrammarNode Next;
	
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}
}