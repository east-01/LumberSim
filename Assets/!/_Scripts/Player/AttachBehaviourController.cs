using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class AttachBehaviourController : MonoBehaviour 
{
    /// <summary>
    /// A list of action settings to be parsed on awake.
    /// The action settings objects' can only be a GameObject (for SetActive() call) or Behaviour
    ///   (for .enabled variable).
    /// </summary>
    [Header("Action setting objects are GameObject or Behaviour")]
    [SerializeField]
    private List<AttachSetting> attachSettings;
    private Dictionary<AttachBehaviour, List<GameObject>> gameObjectAttachSettings;
    private Dictionary<AttachBehaviour, List<Behaviour>> behaviourAttachSettings; 

    private Player player;
    private bool isPaused => player.IsPaused;

    private void Awake()
    {
        player = GetComponent<Player>();

        ParseAttachBehaviours();
        UpdateAttachBehaviours();
    }

    private void ParseAttachBehaviours() 
    {
        gameObjectAttachSettings = new();
        behaviourAttachSettings = new();

        List<UnityEngine.Object> parsedObjects = new();

        foreach(AttachSetting attachSetting in attachSettings) {
            AttachBehaviour behaviour = attachSetting.behaviour;
            UnityEngine.Object obj = attachSetting.obj;

            if(parsedObjects.Contains(obj)) {
                Debug.LogWarning($"Already parsed object \"{obj}\"... skipping");
                continue;
            }

            if(obj is GameObject) {

                if(!gameObjectAttachSettings.ContainsKey(behaviour))
                    gameObjectAttachSettings.Add(behaviour, new());
                
                List<GameObject> gos = gameObjectAttachSettings[behaviour];
                gos.Add(obj as GameObject);
                gameObjectAttachSettings[behaviour] = gos;

            } else if(obj is Behaviour) {

                if(!behaviourAttachSettings.ContainsKey(behaviour))
                    behaviourAttachSettings.Add(behaviour, new());
                
                List<Behaviour> behs = behaviourAttachSettings[behaviour];
                behs.Add(obj as Behaviour);
                behaviourAttachSettings[behaviour] = behs;

            } else {
                Debug.LogError($"Action setting object \"{obj}\" is not a GameObject or Behavior! This is not allowed.");
            }

        }

    }

    public void UpdateAttachBehaviours() 
    {
        Dictionary<AttachBehaviour, bool> states = new() {
            { AttachBehaviour.ACTIVE_ATTACHED, player.LocalPlayer != null },
            { AttachBehaviour.ACTIVE_UNPAUSED, player.LocalPlayer != null && !isPaused},
            { AttachBehaviour.DISABLED_ATTACHED, player.LocalPlayer == null }
        };

        foreach(AttachBehaviour behaviour in states.Keys) {
            if(gameObjectAttachSettings.ContainsKey(behaviour)) {
                foreach(GameObject go in gameObjectAttachSettings[behaviour]) {
                    go.SetActive(states[behaviour]);
                }
            }

            if(behaviourAttachSettings.ContainsKey(behaviour)) {
                foreach(Behaviour beh in behaviourAttachSettings[behaviour]) {
                    beh.enabled = states[behaviour];
                }
            }
        }

    }

    [Serializable]
    public enum AttachBehaviour { 
        ACTIVE_ATTACHED, // The object is active when there is a local player attached
        ACTIVE_UNPAUSED, // The object is active when there is a local player attached, and the pause menu is not shown
        DISABLED_ATTACHED // The object is inactive when there is a local player attached
    }

    [Serializable]
    public struct AttachSetting
    {
        public UnityEngine.Object obj;
        public AttachBehaviour behaviour;
    }
}