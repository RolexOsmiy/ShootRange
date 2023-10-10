using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Random = UnityEngine.Random;

public class BuildingDestruction : DestructionSimulator
{
    [SerializeField] private PositionsPlacementController positionsPlacementController;
    [SerializeField] protected Transform _projectileContainer; // Что будет держать стрелы и болты
    [SerializeField] private ParticleSystem particleSystem;
    [ShowNonSerializedField] protected int _currentHealth;
    [ShowNativeProperty] public int UnitLevel { get; private set; }
    public bool IsDead { get; private set; }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        if (UnitLevel > UnitsData.Instance.maxBuildingLevel)
            UnitLevel = UnitsData.Instance.maxBuildingLevel;
        
        _currentHealth = UnitsData.Instance.levelBuildingToHealth[UnitLevel];
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
                GetComponent<BoxCollider>().enabled = false;
                ragDolls[i].AddExplosionForce(explodePower, explPos, 0);
            }
        }
        else
        {
            for (int i = 0; i < ragDolls.Count; i++)
            {
                ragDolls[i].isKinematic = false;
                ragDolls[i].GetComponent<Collider>().isTrigger = false;
                GetComponent<BoxCollider>().enabled = false;
                ragDolls[i].AddExplosionForce(explodePower, transform.position, 0);
            }
        }
        
        //TODO positionsPlacementController stuff
    }
    
    public void ApplyDamage(int damage, Transform objectToHold = null)
    {
        if(objectToHold != null)
            objectToHold.SetParent(_projectileContainer);

        _currentHealth -= damage;
        CheckHealth(objectToHold);
        
        TapticManager.Impact(TapticImpact.Light);
    }
    
    private void CheckHealth(Transform hitObject) // check if needed to remove someelements or die
    {
        if (_currentHealth <= 0 && !IsDead)
        {
            Die();
            SimulateDeathRagdoll(hitObject.position);
        }
        else
        {
            Debug.Log("Already dead");
            //TODO some hit effect
        }
    }
    
    private void Die()
    {
        IsDead = true;
        transform.Rotate(Vector3.up, Random.Range(-90, 90));
        GetComponent<Collider>().enabled = false;
        SoundManager.Instance.PlayDeathSound();
        
        MainGameManager.Instance.KillAllEnemies();

        if (particleSystem)
            particleSystem.Play();
        //_renderer.material = _deadMat;
    }
}
