using System;
using DG.Tweening;
using NaughtyAttributes;
using QFSW.MOP2;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class ArrowController : MonoBehaviour
{
    public enum ArrowBelonging
    {
        Player,
        Enemy
    }

    public Action OnFlyFinish;
    
    [SerializeField] private Transform  _arrowObject;
    [SerializeField] private Transform  _vibrationContainer;
    [SerializeField] private Collider   _selfCollider;
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private GameObject _trail;
    [SerializeField] private float      _rotationSpeed;
    [SerializeField] private float      _continueFlySpeed; // spped arrow will fly if no trigger found
    [SerializeField] private float      _maxTimeToContinueFly;
    [SerializeField] private PoolableMonoBehaviour _poolableMonoBehaviour;

    [HorizontalLine(color: EColor.Red)] 
    [SerializeField] private int damage;

    private bool      _flying;
    private bool      _continueFlying; // arrow flyes after path finished and no trigger met
    private float     _timeLeftToContinueFly;
    private Tween     _flyTween;
    private Tween     _afterFlyActionTween;
    private float     _currentRotationSpeed;

    [FormerlySerializedAs("arrowBeloning")] public ArrowBelonging arrowBelonging = ArrowBelonging.Player;
    
    #region Init

    private void OnEnable()
    {
        MainGameManager.OnNeedToRemoveOldArrows += OnNeedToRemoveOldArrow;
    }

    private void OnDisable()
    {
        _selfCollider.enabled = false;
        MainGameManager.OnNeedToRemoveOldArrows -= OnNeedToRemoveOldArrow;
    }

    private void Update()
    {
        if(_flying || _continueFlying)
            _arrowObject.Rotate(0,_currentRotationSpeed * Time.deltaTime,0);

        if (_continueFlying)
        {
            _timeLeftToContinueFly -= Time.deltaTime;
            
            if(_timeLeftToContinueFly <= 0)
                StopMovingAfterHit();
            
            transform.Translate(Vector3.forward * (Time.deltaTime * _continueFlySpeed));
        }
    }

    #endregion

    public void Fly(Vector3[] trajectory, float flyDuration)
    {
        _trail.SetActive(true);
        _currentRotationSpeed = _rotationSpeed * Random.Range(0.8f, 1.2f);
        _flyTween = transform.DOPath(trajectory, flyDuration, PathType.CatmullRom)
            .SetLookAt(lookAhead: 0.01f)
            .SetEase(Ease.Linear)
            .OnComplete(ContinueFly);
        _flying = true;
        _selfCollider.enabled = true;
    }

    private void StopMovingAfterHit()
    {
        SoundManager.Instance.PlayWoodSound();
        DisableAllActions();
        GetComponent<Collider>().enabled = false;
        Shake();
        OnFlyFinish?.Invoke();
    }

    private void DisableAllActions()
    {
        _flyTween?.Kill();
        _afterFlyActionTween?.Kill();
        _flying = false;
        _continueFlying = false;
        _trail.SetActive(false);
    }

    private void ContinueFly()
    {
        _continueFlying = true;
        _timeLeftToContinueFly = _maxTimeToContinueFly;
        //_continueFlySpeed = _currentFlyVector.magnitude;
    }
    
    #region Delegates

    private void OnTriggerEnter(Collider other)
    {
        if((arrowBelonging == ArrowBelonging.Player && other.CompareTag("Player")) || 
           (arrowBelonging == ArrowBelonging.Enemy && other.CompareTag("Enemy")))
            return;
        
        var unit = other.GetComponent<UnitController>();
        var building = other.GetComponent<BuildingDestruction>();
        
        if(unit != null && unit.IsDead)
            return; // В мертвых не втыкаемся
        
        StopMovingAfterHit();

        other.GetComponent<IAnimateHit>()?.AnimateHit(transform);

        if(unit != null)
            unit.ApplyDamage(damage, transform); // Заодно прикрепит стрелу к голове

        if (building != null)
            building.ApplyDamage(damage, transform);

        var randomPercent = Random.Range(0, 3);
        if (randomPercent == 0 && particleSystem)
        {
            particleSystem.Play();
        }
    }

    private void OnNeedToRemoveOldArrow()
    {
        DisableAllActions();
        _poolableMonoBehaviour.Release();    
    }

    #endregion

    [Button]
    private void Shake()
    {
        _vibrationContainer.DOShakeRotation(0.35f, 7f, 20, 70, randomnessMode: ShakeRandomnessMode.Harmonic);
    }
}
