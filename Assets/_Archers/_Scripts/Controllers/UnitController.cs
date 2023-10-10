using System;
using System.Collections;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class UnitController : MonoBehaviour
{
    protected static readonly int kAnimatorPrepare = Animator.StringToHash("Prepare");
    protected static readonly int kAnimatorShoot = Animator.StringToHash("Shoot");
    protected static readonly int kAnimatorIdleSpeed = Animator.StringToHash("IdleSpeed");
    protected static readonly int kAnimatorDie = Animator.StringToHash("Die");
    protected static readonly int kAnimatorRun = Animator.StringToHash("Run");
    protected static readonly int kAnimatorRunSpeed = Animator.StringToHash("RunSpeed");
    protected static readonly int kAnimatorCheer = Animator.StringToHash("Cheer");
    protected static readonly int kAnimatorHitIndex = Animator.StringToHash("HitIndex");
    protected static readonly int kAnimatorGetHit = Animator.StringToHash("GetHit");

    protected Animator              _anim;
    protected bool                  _running;
    [ShowNonSerializedField] protected int                   _currentHealth;
    [ShowNonSerializedField] protected int                   _arrowsNumber = 1;
    
    public bool IsDead { get; private set; }
    [ShowNativeProperty] public int UnitLevel { get; private set; }
    public Action<UnitController> onDeath;

    [SerializeField] protected Transform _arrowAppearPoint;
    [SerializeField] protected Transform _tiltBone;
    [SerializeField] protected Transform _projectileContainer; // Что будет держать стрелы и болты на теле
    [SerializeField] protected Vector3   _downTilt; // Юнит смотрит вниз
    [SerializeField] protected Vector3   _middleTilt; 
    [SerializeField] protected Vector3   _upTilt; // Юнит смотрит вверх
    [Space]
    [SerializeField] protected GameObject[] _arrowsOnBow;
    [Space] 
    [SerializeField] protected GameObject[] _helmets;
    [SerializeField] protected GameObject[] _weapons;
    [SerializeField] protected GameObject[] _shoulders;
    [SerializeField] protected GameObject[] _shields;

    [Space] 
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Material _deadMat;
    [Space] 
    [SerializeField] private RagdollController ragdollController;

    #region Init

    private void Awake()
    {
        _anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _anim.SetFloat(kAnimatorIdleSpeed, Random.Range(0.9f,1.1f));
        UpdateUnitLevel();
    }

    #endregion

    #region Public methods

    public void ApplyDamage(int damage, Transform objectToHold = null)
    {
        if(objectToHold != null)
            objectToHold.SetParent(_projectileContainer);

        _currentHealth -= damage;
        CheckHealth(objectToHold);
        
        TapticManager.Impact(TapticImpact.Light);
    }

    [Button("RemoveSome")]
    public void RemoveItems()
    {
        if (_helmets[UnitLevel])
        {
            _helmets[UnitLevel].GetComponent<BoxCollider>().isTrigger = false;
            _helmets[UnitLevel].GetComponent<Rigidbody>().isKinematic = false;
            _helmets[UnitLevel].GetComponent<Rigidbody>().AddExplosionForce(200, _helmets[UnitLevel].transform.position, 0);
            _helmets[UnitLevel].transform.SetParent(null);
            
            HideObjectAndDestroy(_helmets[UnitLevel]); // run destroy tween
        }

        if (_weapons[UnitLevel])
        {
            _weapons[UnitLevel].GetComponent<BoxCollider>().isTrigger = false;
            _weapons[UnitLevel].GetComponent<Rigidbody>().isKinematic = false;
            _weapons[UnitLevel].GetComponent<Rigidbody>().AddExplosionForce(200, _weapons[UnitLevel].transform.position, 0);
            _weapons[UnitLevel].transform.SetParent(null);
            
            HideObjectAndDestroy(_weapons[UnitLevel]); // run destroy tween
        }

        if (_shoulders[UnitLevel])
        {
            _shoulders[UnitLevel].GetComponent<BoxCollider>().isTrigger = false;
            _shoulders[UnitLevel].GetComponent<Rigidbody>().isKinematic = false;
            _shoulders[UnitLevel].GetComponent<Rigidbody>().AddExplosionForce(200, _shoulders[UnitLevel].transform.position, 0);
            _shoulders[UnitLevel].transform.SetParent(null);
            
            HideObjectAndDestroy(_shoulders[UnitLevel]); // run destroy tween
        }

        if (_shields[UnitLevel])
        {
            _shields[UnitLevel].GetComponent<BoxCollider>().isTrigger = false;
            _shields[UnitLevel].GetComponent<Rigidbody>().isKinematic = false;
            _shields[UnitLevel].GetComponent<Rigidbody>().AddExplosionForce(200, _shields[UnitLevel].transform.position, 0);
            _shields[UnitLevel].transform.SetParent(null);
            
            HideObjectAndDestroy(_shields[UnitLevel]); // run destroy tween
        }

        if (UnitLevel > 0)
            UnitLevel--;

        UpdateUnitLevel();
    }

    private void HideObjectAndDestroy(GameObject obj, float delay = 3f)
    {
        DOVirtual.DelayedCall(delay, () =>
        {
            Destroy(obj, 3f);
            obj.transform.DOMove(
                new Vector3(obj.transform.position.x, obj.transform.position.y - 1.5f, obj.transform.position.z), 1f);
        });
    }
    
    public void AnimatePrepareForShooting()
    {
        if(IsDead)
            return;
        
        ShowArrowsOnBow();
        _anim.SetBool(kAnimatorPrepare, true);
    }

    public void AnimateShooting(float normalizedPower = 0f)
    {
        const float kShootDuration = 0.1f + 0.701f / 2 + 0.15f + 0.2f;
        _anim.SetBool(kAnimatorPrepare, false);
        _anim.SetTrigger(kAnimatorShoot);
        HideArrowsOnBow();
        DOVirtual.DelayedCall(kShootDuration, SwitchToMiddleTilt); // DEBUG
    }
    
    public void SetTilt(float normalizedTilt , float normalizedPower = 0)
    {
        if(IsDead)
            return;
        
        var newRotation = Vector3.Lerp(_downTilt, _upTilt, normalizedTilt);
        _tiltBone.localRotation = Quaternion.Euler(newRotation);
        
        _anim.SetLayerWeight(1, normalizedPower); //weight control
    }

    public void Cheer()
    {
        _anim.SetTrigger(kAnimatorCheer);
    }

    public void LookTo(Vector3 targetPoint)
    {
        if(IsDead)
            return;
        
        StartCoroutine(LookToRoutine(targetPoint));
    }

    public int GetArrowsNumber()
    {
        return _arrowsNumber;
    }

    public void SetUnitLevel(int lev)
    {
        UnitLevel = lev;
        UpdateUnitLevel();
    }

    public void UpdateUnitLevel()
    {
        // view
        // health
        // arrows number
        _currentHealth = UnitsData.Instance.levelToHealth[UnitLevel];
        _arrowsNumber = UnitsData.Instance.levelToArrows[UnitLevel];
        SetHelmet(UnitLevel);
        SetWeapon(UnitLevel);
        SetShoulders(UnitLevel);
        SetShield(UnitLevel);
    }

    #endregion

    #region Private methods

    private void ShowArrowsOnBow()
    {
        for (int i = 0; i < _arrowsOnBow.Length; i++)
            _arrowsOnBow[i].SetActive(true);
    }

    private void HideArrowsOnBow()
    {
        for (int i = 0; i < _arrowsOnBow.Length; i++)
            _arrowsOnBow[i].SetActive(false);
    }

    private void CheckHealth(Transform hitObject) // check if needed to remove someelements or die
    {
        if (_currentHealth <= 0)
        {
            Die();
            //ragdollController.SimulateDeathRagdoll(hitObject.position);
        }
        else
        {
            var hitIndexAnim = Random.Range(0, 2);
            _anim.SetInteger(kAnimatorHitIndex, hitIndexAnim);
            _anim.SetTrigger(kAnimatorGetHit);
        }
        
        RemoveItems();
    }

    private void Die()
    {
        IsDead = true;
        transform.Rotate(Vector3.up, Random.Range(-90, 90));
        _anim.SetTrigger(kAnimatorDie);
        GetComponent<Collider>().enabled = false;
        SoundManager.Instance.PlayDeathSound();
        _renderer.material = _deadMat;
        onDeath?.Invoke(this);
    }

    private IEnumerator LookToRoutine(Vector3 targetPoint)
    {
        yield return new WaitUntil(() => !_running);
        
        transform.LookAt(targetPoint, Vector3.up);
        transform.localRotation = Quaternion.Euler(0, transform.localRotation.eulerAngles.y, 0);
    }

    private void SetHelmet(int itemIndex)
    {
        for (int i = 0; i < _helmets.Length; i++)
        {
            if(_helmets[i] != null)
                _helmets[i].SetActive(false);
        }
        
        if (itemIndex < _helmets.Length && _helmets[itemIndex] != null)
            _helmets[itemIndex].SetActive(true);
    }
    
    private void SetWeapon(int itemIndex)
    {
        for (int i = 0; i < _weapons.Length; i++)
        {
            if(_weapons[i] != null)
                _weapons[i].SetActive(false);
        }
        
        if (itemIndex < _weapons.Length && _weapons[itemIndex] != null)
            _weapons[itemIndex].SetActive(true);
    }
    
    private void SetShield(int itemIndex)
    {
        for (int i = 0; i < _shields.Length; i++)
        {
            if(_shields[i] != null)
                _shields[i].SetActive(false);
        }
        
        if (itemIndex < _shields.Length && _shields[itemIndex] != null)
            _shields[itemIndex].SetActive(true);
    }
    
    private void SetShoulders(int itemIndex)
    {
        for (int i = 0; i < _shoulders.Length; i++)
        {
            if(_shoulders[i] != null)
                _shoulders[i].SetActive(false);
        }
        
        if (itemIndex < _shoulders.Length && _shoulders[itemIndex] != null)
            _shoulders[itemIndex].SetActive(true);
    }
    
    #endregion

    #region Debug methods

    [Button]
    public void SetRandomHelmet()
    {
        SetHelmet(Random.Range(0, _helmets.Length));
    }

    [Button()]
    public void SetRandomWeapon()
    {
        SetWeapon(Random.Range(0, _weapons.Length));
    }
    
    [Button()]
    public void SetRandomShoulders()
    {
        SetShoulders(Random.Range(0, _shoulders.Length));
    }
    
    [Button()]
    public void SetRandomShield()
    {
        SetShield(Random.Range(0, _shields.Length));
    }
    
    [Button()]
    public void SwitchToDownTilt()
    {
        _tiltBone.localRotation = Quaternion.Euler(_downTilt);
    }
    
    [Button()]
    public void SwitchToMiddleTilt()
    {
        _tiltBone.localRotation = Quaternion.Euler(_middleTilt);
    }
    
    [Button()]
    private void SwitchToUpTilt()
    {
        _tiltBone.localRotation = Quaternion.Euler(_upTilt);
    }

    [Button()]
    private void SetDownTilt()
    {
        _downTilt = _tiltBone.localRotation.eulerAngles;
    }
    
    [Button()]
    private void SetUpTilt()
    {
        _upTilt = _tiltBone.localRotation.eulerAngles;
    }
    
    [Button("Up level", EButtonEnableMode.Playmode)]
    public void UpUnitLevel()
    {
        UnitLevel++;
        if (UnitLevel > UnitsData.Instance.maxUnitLevel)
            UnitLevel = UnitsData.Instance.maxUnitLevel;
        UpdateUnitLevel();
    }
    
    [Button("Down level", EButtonEnableMode.Playmode)]
    private void DownUnitLevel()
    {
        UnitLevel--;
        if (UnitLevel < 0)
            UnitLevel = 0;
        UpdateUnitLevel();
    }
    
    #endregion
}
