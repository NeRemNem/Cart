using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugRay : MonoBehaviour
{
    private void OnDrawGizmosSelected() {
        
        var trackMask = 1<<LayerMask.NameToLayer("Track");

        Gizmos.color = Color.red;
        RaycastHit hit;
        Gizmos.DrawRay(transform.position,transform.TransformDirection(Vector3.left));
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.left), out hit, 1.4f
            ,trackMask))
        {
            Debug.Log("HIT");
        }
    }
}
