using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[NodeTint("#ed4245")]
[NodeWidth(200)]
public class EndLoopNode : GrammarNode
{

	[Input] public GrammarNode Input;
	
	protected override void Init() {
		base.Init();
		
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
        if (port.fieldName == "Input") return GetInputValue<GrammarNode>("Input", Input);
        else return null;
    }
}