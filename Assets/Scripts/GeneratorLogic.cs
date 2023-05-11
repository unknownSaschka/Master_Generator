using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratorLogic : MonoBehaviour
{
    public GameObject Level;

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

    public void Generate()
    {
        PrepareCollections();
    }

    private void PrepareCollections()
    {
        //Select 2 - 4 Prefabs
        //Get the Two Places where the next Collections can be Placed

        LevelCollections = new List<GameObject>();
        collectionIndex = 1;

        for(int i = 0; i < 3; i++)
        {
            LevelCollections.Add(GetRandomNormalCollection(CollectionType.Normal));
        }

        LevelCollections.Add(GetRandomNormalCollection(CollectionType.End));

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
}
