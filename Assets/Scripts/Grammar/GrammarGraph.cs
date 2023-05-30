using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;

[CreateAssetMenu]
public class GrammarGraph : NodeGraph {

	private GameObject First;
	private GameObject Parent;

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

	public GameObject GetCollections(GameObject preparingParent)
	{
		First = null;
		Parent = preparingParent;

        var startNodes = GetStartNodes();
		GrammarNode firstNode = startNodes.First().GetOutputPort("Next").GetConnections().First().node as GrammarNode;

		GetNextCollection(firstNode, null, 0);
		return First;
    }

	private void GetNextCollection(GrammarNode node, GameObject previous, int spawnPoint)
	{
		//TDOD TESTING AAAAAAAAAAAAHHHHHHHHHHHHHHHHHHH

		if(node is CollectionLibrary)
		{
			string folder = (node as CollectionLibrary).GetSelectedFolder();
			GameObject current = Instantiate(GetRandomFromFolder(folder), Parent.transform);

			if(First == null)
			{
				First = current;
			}

			if(previous != null)
			{
                CollectionSpawner previousCS = previous.GetComponent<CollectionSpawner>();

                if (spawnPoint < previousCS.NextPosition.Count)
                {
					previousCS.NextGameObjectToSpawn.Add(current);
                }
            }

            //Node logic
            List<NodePort> ports = node.GetOutputPort("Next").GetConnections();

			int index = 0;
            foreach (var port in ports)
			{
				GetNextCollection(port.node as GrammarNode, current, index);
				index++;
			}
        }

		if(node is EndLoopNode)
		{
            CollectionSpawner previousCS = previous.GetComponent<CollectionSpawner>();
			previousCS.NextGameObjectToSpawn.Add(First);
        }
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

	private GameObject GetRandomFromFolder(string folder)
	{
        string[] guids = AssetDatabase.FindAssets("t:prefab", new string[] { folder });
        int rng = UnityEngine.Random.Range(0, guids.Length - 1);
        string path = AssetDatabase.GUIDToAssetPath(guids[rng]);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

}