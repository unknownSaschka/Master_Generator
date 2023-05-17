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
    private List<Transform> BlockPlacements;
    private int collectionIndex = 1;

    private enum CollectionType { TwoWay, Normal, End }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CompleteGeneration()
    {
        ClearAll();
        PrepareCollections();
        SimulateSteps();
    }

    public void ClearAll()
    {
        DeleteChildren(Level);
        DeleteChildren(PreparingObject);
    }

    public void ClearPrefabs()
    {
        DeleteChildren(Level);
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

        PlaceCollection(start, Level, step);
    }

    private void PlaceCollection(GameObject collection, GameObject spawnPoint, int step)
    {
        if(step > SimulationSteps) { return; }

        GameObject go = PlacePrefab(collection, spawnPoint.transform.position);
        Rotate(spawnPoint.transform.parent?.gameObject, spawnPoint, go);

        CollectionSpawner cs = go.GetComponent<CollectionSpawner>();
        
        List<(GameObject, GameObject)> nextToPlace= new List<(GameObject, GameObject)>();
        for(int i = 0; i < cs.NextGameObjectToSpawn.Count; i++)
        {
            Debug.Log($"{i}, {cs.NextGameObjectToSpawn.Count}");
            nextToPlace.Add((cs.NextGameObjectToSpawn[i], cs.NextPosition[i]));
        }
        
        foreach(var tuple in nextToPlace) 
        {
            PlaceCollection(tuple.Item1, tuple.Item2, ++step);
        }
    }

    public void PrepareCollections()
    {
        ClearAll();
        //Select 2 - 4 Prefabs
        //Get the Two Places where the next Collections can be Placed

        BlockPlacements = new List<Transform>();
        LevelCollectionPrefabs = new List<GameObject>();
        collectionIndex = 1;

        for(int i = 0; i < CollectionSize - 1; i++)
        {
            if(UnityEngine.Random.Range(0, 1) == 0){
                LevelCollectionPrefabs.Add(GetRandomNormalCollection(CollectionType.TwoWay));
            }
            else
            {
                LevelCollectionPrefabs.Add(GetRandomNormalCollection(CollectionType.Normal));
            }
            
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
            case CollectionType.TwoWay:
                collection = "Assets/Prefabs/Collections/Two Way";
                break;
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

    private GameObject PlacePrefab(GameObject go, Vector3 position)
    {
        GameObject newGO = Instantiate(go, Level.transform);
        newGO.transform.position = position;
        BlockPlacements.Add(newGO.transform);
        return newGO;
    }

    private GameObject Rotate(GameObject previous, GameObject spawnLocation, GameObject current)
    {
        DirectionLogic.Direction prevDir;
        if (previous == null)
        {
            prevDir = DirectionLogic.Direction.North;
        }
        else
        {
            prevDir = DirectionLogic.GetDirection(previous.transform);
        }
        
        DirectionLogic.Direction spawnPointDirection = spawnLocation.GetComponent<SpawnPoint>().Directon;
        DirectionLogic.Direction resultingDirection = DirectionLogic.GetRotation(prevDir, spawnPointDirection);
        int newRotation = DirectionLogic.GetRoation(resultingDirection);
        Vector3 oldRotation = current.transform.rotation.eulerAngles;
        current.transform.rotation = Quaternion.Euler(oldRotation.x, newRotation, oldRotation.z);
        return current;
    }
}
