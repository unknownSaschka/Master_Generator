using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratorLogic : MonoBehaviour
{
    public GameObject Level;
    public GameObject PreparingObject;
    public int CollectionSize = 4;
    public int SimulationSteps = 2;

    private List<GameObject> LevelCollectionPrefabs;
    private List<GameObject> LevelCollections;
    private int collectionIndex = 1;

    private enum CollectionType { Normal, End }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ClearAll()
    {
        DeleteChildren(Level);
        DeleteChildren(PreparingObject);
    }

    private void DeleteChildren(GameObject parent)
    {
        var temp = new GameObject[parent.transform.childCount];
        for(int i = 0; i < temp.Length; i++)
        {
            temp[i] = parent.transform.GetChild(i).gameObject;
        }

        foreach(var child in temp)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    public void SimulateSteps()
    {
        int step = 0;
        GameObject start = LevelCollections[0];
        Transform startPosition = Level.transform;

        PlaceCollection(start, startPosition, step);
    }

    private void PlaceCollection(GameObject collection, Transform position, int step)
    {
        if(step > SimulationSteps) { return; }

        GameObject go = PlacePrefab(collection, position);
        CollectionSpawner cs = go.GetComponent<CollectionSpawner>();
        
        List<(Transform, GameObject)> nextToPlace= new List<(Transform, GameObject)>();
        for(int i = 0; i < cs.NextGameObjectToSpawn.Count; i++)
        {
            Debug.Log($"{i}, {cs.NextGameObjectToSpawn.Count}");
            nextToPlace.Add((cs.NextPosition[i].transform, cs.NextGameObjectToSpawn[i]));
        }
        
        foreach(var tuple in nextToPlace) 
        {
            PlaceCollection(tuple.Item2, tuple.Item1, ++step);
        }
    }

    public void PrepareCollections()
    {
        ClearAll();
        //Select 2 - 4 Prefabs
        //Get the Two Places where the next Collections can be Placed

        LevelCollectionPrefabs = new List<GameObject>();
        collectionIndex = 1;

        for(int i = 0; i < CollectionSize - 1; i++)
        {
            LevelCollectionPrefabs.Add(GetRandomNormalCollection(CollectionType.Normal));
        }
        LevelCollectionPrefabs.Add(GetRandomNormalCollection(CollectionType.End));

        LevelCollections = new List<GameObject>();
        foreach(var prefab in LevelCollectionPrefabs)
        {
            GameObject newObject = Instantiate(prefab, PreparingObject.transform);
            LevelCollections.Add(newObject);
        }

        PrepareCollection(LevelCollections[0]);
    }

    private void PrepareCollection(GameObject gameObject)
    {
        CollectionSpawner cs = gameObject.GetComponent<CollectionSpawner>();
        int nextCount = cs.NextPosition.Count;

        List<GameObject> toPrepare = new List<GameObject>();

        for(int i = 0; i < nextCount; i++)
        {
            if(collectionIndex > LevelCollections.Count - 1)
            {
                //When all Collections are already used, set the first collection
                cs.NextGameObjectToSpawn.Add(LevelCollections[0]);
                continue;
            }

            cs.NextGameObjectToSpawn.Add(LevelCollections[collectionIndex]);
            toPrepare.Add(LevelCollections[collectionIndex]);
            collectionIndex++;
            Debug.Log(collectionIndex);
        }

        //prepare the next Collections
        foreach(GameObject go in toPrepare)
        {
            PrepareCollection(go);
        }
    }

    private GameObject GetRandomNormalCollection(CollectionType collectionType)
    {
        string collection = "";

        switch (collectionType)
        {
            case CollectionType.Normal:
                collection = "Assets/Prefabs/Collections/Normal";
                break;
            case CollectionType.End:
                collection = "Assets/Prefabs/Collections/End Collections";
                break;
        }

        string[] guids = AssetDatabase.FindAssets("t:prefab", new string[] { collection });
        int rng = UnityEngine.Random.Range(0, guids.Length - 1);
        string path = AssetDatabase.GUIDToAssetPath(guids[rng]);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private GameObject GetNextUnvisited()
    {
        foreach(GameObject go in LevelCollections)
        {
            if(go.GetComponent<CollectionSpawner>().Visited) return go;
        }

        return null;
    }

    private GameObject PlacePrefab(GameObject go, Transform position)
    {
        GameObject newGO = Instantiate(go, Level.transform);
        newGO.transform.position = position.position;
        return newGO;
    }
}
