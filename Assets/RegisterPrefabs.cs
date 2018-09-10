using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegisterPrefabs : MonoBehaviour {

    public List<GameObject> prefabs;
    bool run = false;
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (!run)
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                NetEngine.instance.SpawnablePrefabs.Add(i, prefabs[i]);
            }
            run = true;
        }
    }
}
