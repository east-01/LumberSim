using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayManager : MonoBehaviour
{

    public static Dictionary<SceneLookupData, GameplayManager> SceneSingleton = new();

    public Scene GameplayScene => gameObject.scene;

    private void Start() 
    {
        SceneSingletons.Register(this);
    }

    private void OnEnable() 
    {
        if(SceneSingleton.ContainsKey(gameObject.scene.GetSceneLookupData())) {
            Debug.LogError("GameplayManager Instance already exists, destroying.");
            return;
        }

        SceneSingleton.Add(gameObject.scene.GetSceneLookupData(), this);

        // if(!InstanceFinder.IsServerStarted) {
        //     Debug.LogWarning("Gameplay manager is enabled without server being active, disabling.");
        //     gameObject.SetActive(false);
        //     return;
        // }

        if(InstanceFinder.IsClientStarted)
            InstanceFinder.ClientManager.RegisterBroadcast<ClientNetworkedScene>(OnClientNetworkedScene);
    }

    private void OnDisable() 
    {
        if(SceneSingleton.ContainsKey(gameObject.scene.GetSceneLookupData()))
            SceneSingleton.Remove(gameObject.scene.GetSceneLookupData());
            
        if(InstanceFinder.IsClientStarted)
            InstanceFinder.ClientManager.UnregisterBroadcast<ClientNetworkedScene>(OnClientNetworkedScene);
    }

    private bool hasSpawnedTree = false;

    private void Update() 
    {
        if(!hasSpawnedTree && InstanceFinder.IsServerStarted) {
            TreeLogGroup grp = SpawnLogObject(new(-19.58f, 0, 1.997f), Quaternion.Euler(45f, 0f, 0f), new(
                5f,
                1f,
                Vector3.up,
                0, 
                new TreeLogData[] {
                    new(3f, 1f, Vector3.right, 0, new TreeLogData[] {
                        new(3f, 1f, new Vector3(1, 1, 0).normalized, 0, new TreeLogData[0])
                    })
                }
            ));
            Destroy(grp.GetComponent<Rigidbody>());

            SpawnTree(new(-19.58f, 0, 11.997f));
            hasSpawnedTree = true;
        }
    }

    private void OnClientNetworkedScene(ClientNetworkedScene msg, Channel channel)
    {
        if(msg.scene.Name != "GameplayScene")
            return;

        // GameplayScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(msg.scene.Name);
    }

    // TODO: Below probably should go in its own TreeManager
    [SerializeField]
    private GameObject logObjectPrefab;
    [SerializeField]
    private GameObject choppableTreePrefab;

    public TreeLogGroup SpawnLogObject(Vector3 position, Quaternion rotation, TreeLogData rootData) 
    {
        if(!InstanceFinder.IsServerStarted) 
            throw new InvalidOperationException("Can't spawn log object, server isn't started.");
    
        GameObject logObject = Instantiate(logObjectPrefab);

        TreeLogGroup log = logObject.GetComponent<TreeLogGroup>();
        logObject.transform.SetPositionAndRotation(position, rotation);
        log.SetRootData(rootData);

        InstanceFinder.ServerManager.Spawn(logObject, null, gameObject.scene);

        // logObject.transform.position = position;

        return log;
    }

    public ChoppableTree SpawnTree(Vector3 position) 
    {
        if(!InstanceFinder.IsServerStarted)
            throw new InvalidOperationException("Can't spawn tree, server isn't started");
        
        GameObject treeObject = Instantiate(choppableTreePrefab);

        InstanceFinder.ServerManager.Spawn(treeObject, null, gameObject.scene);

        treeObject.transform.position = position;

        return treeObject.GetComponent<ChoppableTree>();
    }

}
