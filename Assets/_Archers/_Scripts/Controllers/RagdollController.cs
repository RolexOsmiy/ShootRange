using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class RagdollController : DestructionSimulator
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.enabled = true;
        
        ragDolls.AddRange(GetComponentsInChildren<Rigidbody>());

        for (int i = 0; i < ragDolls.Count; i++)
        {
            ragDolls[i].isKinematic = true;
            ragDolls[i].GetComponent<Collider>().isTrigger = true;
        }
    }
    
    [Button("Ragdoll")]
    public void SimulateDeathRagdoll()
    {
        for (int i = 0; i < ragDolls.Count; i++)
        {
            ragDolls[i].isKinematic = false;
            ragDolls[i].GetComponent<Collider>().isTrigger = false;
            ragDolls[i].AddExplosionForce(explodePower, transform.position, 0);
        }

        animator.enabled = false;
    }
}
