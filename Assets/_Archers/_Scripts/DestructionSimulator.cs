using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class DestructionSimulator : MonoBehaviour
{
    [SerializeField] protected List<Rigidbody> ragDolls = new List<Rigidbody>();
    [SerializeField] protected float explodePower = 3000;
    

    private void Awake()
    {
        ragDolls.AddRange(GetComponentsInChildren<Rigidbody>());

        for (int i = 0; i < ragDolls.Count; i++)
        {
            ragDolls[i].isKinematic = true;
            ragDolls[i].GetComponent<Collider>().isTrigger = true;
        }
    }

    [Button("Ragdoll")]
    public void SimulateDeathRagdoll(Vector3 explPos = default)
    {
        if (explPos != Vector3.zero)
        {
            for (int i = 0; i < ragDolls.Count; i++)
            {
                ragDolls[i].isKinematic = false;
                ragDolls[i].GetComponent<Collider>().isTrigger = false;
                ragDolls[i].AddExplosionForce(explodePower, explPos, 0);
            }
        }
        else
        {
            for (int i = 0; i < ragDolls.Count; i++)
            {
                ragDolls[i].isKinematic = false;
                ragDolls[i].GetComponent<Collider>().isTrigger = false;
                ragDolls[i].AddExplosionForce(explodePower, transform.position, 0);
            }
        }
    }
}
