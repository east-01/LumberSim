using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using EMullen.Core;
using GameKit.Dependencies.Utilities;
using TreeEditor;
using UnityEngine;

/// <summary>
/// The TreeLog acts as a singular log object for on the tree, a part ofTreeLogGroups.
/// Initialized with some data, the TreeLog object will reflect this data.
/// </summary>
public class TreeLog : MonoBehaviour
{
    // private ChoppableTree tree;
    // public GameObject LogObject { get; private set;}
    /// <summary>
    /// The GameObject that has the logs mesh and collider.
    /// </summary>
    [SerializeField]
    private GameObject logObject;
    public GameObject LogObject => logObject;

    [SerializeField]
    private TreeLogData data;
    public TreeLogData Data => data;

    [SerializeField]
    private Vector3 angleReadout;
    // public Vector3 Angle { get; private set; }    
    private Vector3 CalculateEndpoint(Vector3 baseDirection, int dir) => (Quaternion.Euler(Data.angle) * baseDirection).normalized * (Data.length / 2f) * dir;
    public Vector3 LocalEndpointForward => CalculateEndpoint(Vector3.up, 1);
    public Vector3 LocalEndpointBackward => CalculateEndpoint(Vector3.up, -1);
    // public Vector3 EndpointForward => logObject.transform.position + LogGroup.transform.rotation * CalculateEndpoint(Vector3.up, 1);
    // public Vector3 EndpointBackward => logObject.transform.position + LogGroup.transform.rotation * CalculateEndpoint(Vector3.up, -1);

    public TreeLog Parent { get {
        if(transform.parent == null)
            return null;
        return transform.parent.GetComponent<TreeLog>();
    } }
    public TreeLogGroup LogGroup => GetComponentInParent<TreeLogGroup>();

    public TreeLog[] ChildBranches { get {
        List<TreeLog> childBranches = new();
        for(int i = 0; i < transform.childCount; i++) {
            Transform child = transform.GetChild(i);
            if(child.TryGetComponent(out TreeLog childLog))
                childBranches.Add(childLog);
        }
        return childBranches.ToArray();
    } }
    
    public void Initialize(TreeLogData data) 
    {
        if(logObject.GetComponent<CapsuleCollider>() == null) {
            Debug.LogError($"Cannot wrap GameObject named \"{logObject.name}\" as TreeBranch: It doesn't have a CapsuleCollider.");
            return;
        }
        if(data.length < 0.15f) {
            Debug.LogWarning("Log length is under length limit. Deleting self.");
            Destroy(gameObject);
            return;
        }

        SetData(data);
    }

    public void SetData(TreeLogData data) 
    {
        this.data = data;

        if(Parent == null) {
            transform.localPosition = Vector3.zero;
            LogGroup.SetRoot(this);
        } else {
            // transform.position = LogGroup.GetFrontEndpoint(Parent.GetIdentifierPath());
            transform.localPosition = Parent.Data.angle.normalized*Parent.Data.length;
        }

        logObject.transform.localPosition = Data.angle*(Data.length/2);
        // logObject.transform.forward = /*LogGroup.transform.rotation * */Data.angle;
        logObject.transform.localRotation = Quaternion.LookRotation(Data.angle);

        MeshFilter meshFilter = logObject.GetComponent<MeshFilter>();
        float meshLength = meshFilter.mesh.bounds.size.z;

        CapsuleCollider capsuleCollider = logObject.GetComponent<CapsuleCollider>();
        capsuleCollider.height = meshLength;

        Vector3 transformScale = logObject.transform.GetScale();
        transformScale.z = data.length/meshLength;
        logObject.transform.SetScale(transformScale);
    }

    /// <summary>
    /// A path of sibling indices for this specific TreeLog GameObject to be identified to be used
    ///   over the network.
    /// </summary>
    public int[] GetIdentifierPath() 
    {
        List<int> indices = new();
        TreeLog focus = this;
        while(focus.Parent != null) {
            indices.Add(focus.data.siblingIndex);
            focus = focus.Parent;
        }

        indices.Reverse();
        
        return indices.ToArray();
    }

    private void OnDrawGizmos() 
    {
        if(!gameObject.activeSelf || logObject == null || LogGroup == null)
            return;

        Gizmos.color = Color.cyan;
        Vector3 forwardEndpoint = LogGroup.GetFrontEndpoint(GetIdentifierPath());
        Gizmos.DrawWireSphere(forwardEndpoint, 0.5f);
        // Gizmos.DrawWireSphere(EndpointBackward, 0.2f);
        Gizmos.DrawLine(logObject.transform.position, forwardEndpoint);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(LocalEndpointForward, 0.5f);
        Gizmos.DrawWireSphere(LocalEndpointBackward, 0.2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(logObject.transform.position, 0.5f);
        Gizmos.DrawLine(logObject.transform.position, logObject.transform.position+Data.angle);
    }
}

[Serializable]
public struct TreeLogData 
{
    public float length;
    public float radius;
    public Vector3 angle;
    public TreeLogData[] children;
    public int siblingIndex;
    public TreeLogData(float length, float radius, Vector3 angle, int siblingIndex, TreeLogData[] children) 
    {
        this.length = length;
        this.radius = radius;
        this.angle = angle;
        this.children = children;
        this.siblingIndex = siblingIndex;
    }

    public TreeLogData Clone() 
    {
        return new(
            length,
            radius,
            angle,
            siblingIndex,
            children
        );
    }

    public void RemoveChild(int idx) {
        TreeLogData[] newChildren = new TreeLogData[children.Length-1];
        for(int i = 0; i < children.Length-1; i++) {
            int offset = i >= idx ? 1 : 0;
            newChildren[i] = children[i + offset];
        }
        this.children = newChildren;
    }

    public override bool Equals(object obj)
    {
        if(obj == null)
            return false;
        if(obj is not TreeLogData)
            return false;
        TreeLogData other = (TreeLogData) obj;

        return length == other.length && radius == other.radius && angle == other.angle && children == other.children && siblingIndex == other.siblingIndex;
    }
    public override int GetHashCode() => base.GetHashCode();
}