using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateAssetMenu]
public class GrammarGraph : NodeGraph { 
	
	public GameObject GetLevel()
	{
		var startNodes = GetStartNodes();

		foreach (var node in startNodes)
		{
			Debug.Log("Start Node");
			GetNextNodes(node);
		}

		return null;
	}

	private List<GrammarNode> GetNextNodes(GrammarNode node)
	{
		List<NodePort> ports = node.GetOutputPort("Next").GetConnections();
		List<GrammarNode> nextNodes = new List<GrammarNode>();
		
		foreach(var port in ports)
		{
			GrammarNode nextNode = port.node as GrammarNode;
			nextNodes.Add(nextNode);

			if(nextNode is CollectionLibrary)
			{
				Debug.Log((nextNode as CollectionLibrary).Selected);
				GetNextNodes(nextNode);
			}

			if(nextNode is EndLoopNode)
			{
				Debug.Log("End Node");
			}
		}

		return nextNodes;
	}


	private List<StartNode> GetStartNodes()
	{
		List<StartNode> startNodes = new List<StartNode>();

		foreach(GrammarNode node in nodes)
		{
			if(node is StartNode)
			{
				startNodes.Add(node as StartNode);
			}
		}

		return startNodes;
	}

}