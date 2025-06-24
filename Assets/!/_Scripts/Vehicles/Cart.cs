using System.Collections;
using System.Collections.Generic;
using System.Linq;
using sc.terrain.proceduralpainter;
using UnityEngine;

public class Cart : MonoBehaviour
{
    private readonly HashSet<Collider> intersectingColliders = new HashSet<Collider>();
    public IReadOnlyCollection<Collider> IntersectingColliders => intersectingColliders;

    private Rigidbody rb;

    private void Start()
    {
        // rb = GetComponent<Rigidbody>();   

        // rb.useGravity = false;
    }

    private void Update()
    {        
        // Vector3 direction = Vector3.zero;
        // if(Input.GetKeyDown(KeyCode.W)) {
        //     direction = transform.forward;
        // } else if(Input.GetKeyDown(KeyCode.A)) {
        //     direction = -transform.right;
        // } else if(Input.GetKeyDown(KeyCode.S)) {
        //     direction = -transform.forward;
        // } else if(Input.GetKeyDown(KeyCode.D)) {
        //     direction = transform.right;
        // }

        // ApplyForce(direction);
    }

    private void OnTriggerEnter(Collider other)
    {
        intersectingColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        intersectingColliders.Remove(other);
    }   

    public void ApplyForce(Vector3 force)     
    {
        if(force == Vector3.zero)
            return;

        rb.AddForce(force, ForceMode.VelocityChange);
        intersectingColliders.ToList().ForEach(ic => ic.attachedRigidbody.AddForce(force, ForceMode.VelocityChange));
    }
}
