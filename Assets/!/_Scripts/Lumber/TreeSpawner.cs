using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;

public class TreeSpawner : MonoBehaviour, IS3
{
    private static bool hasWarned = false;
    private GameplayManager gameplayManager;

    private ChoppableTree tree;

    // Start is called before the first frame update
    void Start()
    {
        if(!InstanceFinder.IsServerStarted) {
            if(!hasWarned) {
                Debug.LogWarning("Server isn't started. Destroying TreeSpawner self");
                hasWarned = true;
            }
            Destroy(gameObject);
            return;
        }

        SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

        if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
            SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(gameplayManager != null && tree == null) {
            tree = gameplayManager.SpawnTree(transform.position);
        }
    }

    public void SingletonRegistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager = singleton as GameplayManager;
    }

    public void SingletonDeregistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

    }
}
