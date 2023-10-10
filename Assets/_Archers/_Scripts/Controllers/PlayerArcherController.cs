using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class PlayerArcherController : UnitController
{
    private const float kStandartRunSpeed = 5f;
    private static readonly int kAnimatorFloating = Animator.StringToHash("Floating");
    
    [SerializeField] private NavMeshAgent _navMeshAgent;
    
    public int positionInPlacement;
    public Action onRunFinish;

    #region Events

    public static event Action<PlayerArcherController> OnUnitSelected; 
    public static event Action<PlayerArcherController> OnUnitDrag; 
    public static event Action<PlayerArcherController> OnUnitDeselected; 

    #endregion

    #region Update

    private void Update()
    {
        if(_running && _navMeshAgent.enabled)
        {
            if (_navMeshAgent.remainingDistance <= 0 && !_navMeshAgent.pathPending) 
                Stop();
        }
    }

    #endregion
    
    
    #region Fly

    private void FlyUp()
    {
        // TODO: Implement
        _anim.SetBool(kAnimatorFloating, true);
    }

    private void FlyDown()
    {
        // TODO: Implement
        _anim.SetBool(kAnimatorFloating, false);
    }

    #endregion

    #region Run

    private void Stop()
    {
        _running = false;
        _navMeshAgent.enabled = false;
        _anim.SetBool(kAnimatorRun, false);
        onRunFinish?.Invoke();
    }

    public void RunToPoint(Vector3 destination)
    {
        var speedModifier = Random.Range(0.8f, 1.2f);
        
        _running = true;
        _anim.SetFloat(kAnimatorRunSpeed, speedModifier);
        _navMeshAgent.speed = kStandartRunSpeed * speedModifier;

        _navMeshAgent.enabled = true;
        _navMeshAgent.destination = destination;
        _anim.SetBool(kAnimatorRun, true);
    }

    #endregion

    #region Touches

    private void OnMouseDown()
    {
        if(MainGameManager.Instance.GameState != GameState.UpgradingUnits)
            return;
        
        FlyUp();
        OnUnitSelected?.Invoke(this);
    }

    private void OnMouseDrag()
    {
        if(MainGameManager.Instance.GameState != GameState.UpgradingUnits)
            return;

        OnUnitDrag?.Invoke(this);
    }

    private void OnMouseUp()
    {
        if(MainGameManager.Instance.GameState != GameState.UpgradingUnits)
            return;
        
        FlyDown();
        OnUnitDeselected?.Invoke(this);
    }
    
    #endregion
}
