using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;

/// <summary>
/// The TreeSpawner class is responsible for spawning a tree at this transform's position once the
///   server starts. In the future the settings can be applied here instead of on the ChoppableTree
///   prefab.
/// </summary>
public class TreeSpawner : MonoBehaviour, IS3
{
    /// <summary>
    /// Has sent the warning for destroying self if on client instance.
    /// </summary>
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

    void Update()
    {
        // Spawn the tree if the gameplay manager exists and it hasn't already been spawned.
        if(gameplayManager != null && tree == null) {
            tree = gameplayManager.SpawnTree(transform.position);
        }
    }
}
