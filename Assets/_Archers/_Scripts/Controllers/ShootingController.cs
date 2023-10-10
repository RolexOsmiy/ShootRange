using System;
using System.Collections;
using System.Collections.Generic;
using QFSW.MOP2;
using UnityEngine;
using Random = UnityEngine.Random;

public enum Side
{
    Player,
    Enemy
}

public class ShootingController : MonoBehaviour
{
    private const float kArrowAppearRadius = 0f; // Radius around start point to appear new arrows
    private const float kShootPauseMin = 0.05f;
    private const float kShootPauseMax = 0.1f;
    private const float kArrowSoundChance = 0.3f;

    [SerializeField] private DrawTrajectory _drawTrajectory;
    
    private Stack<ArrowController> _currentArrowCtrls;
    private Vector3                _playersArrowStartPoint;
    private Vector3                _enemiesArrowStartPoint;
    private int                    _arrowsInTheAir;
    private Action                 _onAllArrowsFinished;
    
    #region Init

    private void Start()
    {
        _currentArrowCtrls = new Stack<ArrowController>();
    }

    #endregion

    #region Shooting methods

    public void BasicShootWithCircleSpreadingForEnemies(int arrowsNumber, Vector3 endPoint, float height, float standartFlyDuration, float spreading, Action onAllArrowsFinished = null)
    {
        // Creating additional arrows
        _arrowsInTheAir = arrowsNumber;
        _onAllArrowsFinished = onAllArrowsFinished;
        CreateNewArrows(arrowsNumber, Side.Enemy);
        StartCoroutine(BasicShootWithCircleSpreadingForEnemiesCoroutine(endPoint, height, standartFlyDuration, spreading));
        SoundManager.Instance.PlayArrowSound();
    }

    /*public void BasicShootWithCircleSpreadingForPlayerArchers(int arrowsNumber, float standartFlyDuration, float spreading)
    {
        // Creating additional arrows
        CreateNewArrows(arrowsNumber, Side.Player);
        StartCoroutine(BasicShootArrowsWithPauseAndCircleSpreadingForPlayersCoroutine(standartFlyDuration, spreading));
    }

    public void LineShootNowForPlayer(int arrowsNumber, float standartFlyDuration, Vector3 leftPoint, float abMagnitude, Vector3 abDirection) // oval shoot
    {
        // Creating additional arrows
        CreateNewArrows(arrowsNumber, Side.Player);
        
        TapticManager.Impact(TapticImpact.Heavy);
        //StartCoroutine(LineShootNowForPlayerCoroutine(standartFlyDuration, leftPoint, abMagnitude, abDirection));
        SoundManager.Instance.PlayArrowSound();
    }*/

    public void ShootNowForPlayerWithDiffAngle(int arrowsNumber, float standartFlyDuration, Vector3 direction, float normPower, float normAngle, Action onAllArrowsFinished = null)
    {
        // Creating additional arrows
        _arrowsInTheAir = arrowsNumber;
        _onAllArrowsFinished = onAllArrowsFinished;
        CreateNewArrows(arrowsNumber, Side.Player);
        TapticManager.Impact(TapticImpact.Heavy);
        StartCoroutine(ShootNowForPlayerDiffAngleCoroutine(standartFlyDuration, direction, normPower, normAngle));
        SoundManager.Instance.PlayArrowSound();
    }
    
    public void GroupShootForPlayers(List<int> arrowGroups, List<Transform> targets, float flyDuration, float normPower, float normAngle, Action onAllArrowsFinished = null)
    {
        int GetArrowsNumber(IReadOnlyList<int> arrowsGr)
        {
            int n = 0;
            for (int i = 0; i < arrowsGr.Count; i++)
                n += arrowsGr[i];
            return n;
        }
        
        _arrowsInTheAir = GetArrowsNumber(arrowGroups);
        _onAllArrowsFinished = onAllArrowsFinished;
        CreateNewArrows(_arrowsInTheAir, Side.Player);
        
        var currentTargetIndex = 0;
        var targetsCenter = GetCenterOfTargets(targets);
        var shootDirection = (targetsCenter - _playersArrowStartPoint).normalized;
        for (int i = 0; i < arrowGroups.Count; i++)
        {
            if (currentTargetIndex < targets.Count) // there are free targets
            {
                // TODO: Пока без вероятности
                StartCoroutine(BasicShootArrowsPackToTargetCoroutine(arrowGroups[i], targets[currentTargetIndex], normPower, normAngle, flyDuration));
                currentTargetIndex++;
            }
            else // no targets left, shooting just regular spreading
            {
                StartCoroutine(ShootNowForPlayerDiffAngleCoroutine(flyDuration, shootDirection, normPower, normAngle));
            }
        }
        
        SoundManager.Instance.PlayArrowSound();
    }
    
    #endregion

    #region Public methods

    public void Init()
    {
        _currentArrowCtrls.Clear();
    }

    public void SetArrowsAppearPoints(Vector3 enemyPoint, Vector3 playerPoint)
    {
        _enemiesArrowStartPoint = enemyPoint;
        _playersArrowStartPoint = playerPoint;
    }

    #endregion

    #region Shooting coroutines

    private IEnumerator BasicShootArrowsPackToTargetCoroutine(int arrowsNumber, Transform target, float normPower, float normAngle, float flyDuration)
    {
        const float kMinimalSpreadingOntarget = 0.5f;
        const float kTargetY = 1f; // target half height
        const float kTargetPowerFactor = 1f; // сколько добавить, чтобы стрела пошла чуть выше подножия цели
        
        var targetPos = target.position;
        while (_currentArrowCtrls.Count != 0 && arrowsNumber > 0)
        {
            var startPoint = GetStartPointInRadius(_playersArrowStartPoint, kArrowAppearRadius);
            var direction = (targetPos - startPoint).normalized;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            var tempEndPoint = targetPos;
            var path = _drawTrajectory.GeneratePath(startPoint, direction, normPower * kTargetPowerFactor, normAngle);
            
            ShootOneArrow(flyDuration, startPoint, path);
            arrowsNumber--;
            yield return new WaitForSeconds(Random.Range(kShootPauseMin, kShootPauseMax));
        }
    }

    private IEnumerator ShootNowForPlayerDiffAngleCoroutine(float standartFlyDuration, Vector3 defaultDirection, float normPower, float normAngle)
    {
        const float kMaxRandomAngle = 10f; // максимальный угол влево-вправо, для разброса стрел

        while (_currentArrowCtrls.Count != 0)
        {
            var startPoint = GetStartPointInRadius(_playersArrowStartPoint, kArrowAppearRadius);
            var newDirection = Quaternion.Euler(0, Random.Range(-kMaxRandomAngle, kMaxRandomAngle), 0) * defaultDirection;
            var path = _drawTrajectory.GeneratePath(startPoint, newDirection, normPower, normAngle);

            ShootOneArrow(standartFlyDuration, startPoint, path);
            yield return new WaitForSeconds(Random.Range(kShootPauseMin, kShootPauseMax));
        }
    }

    private IEnumerator BasicShootWithCircleSpreadingForEnemiesCoroutine(Vector3 endPoint, float height, float flyDuration, float spreading)
    {
        while (_currentArrowCtrls.Count != 0)
        {
            var startPoint = GetStartPointInRadius(_enemiesArrowStartPoint, kArrowAppearRadius);
            var tempEndPoint = endPoint;
            var randomOffsetEndPoint =
                Random.insideUnitCircle * spreading; // final offset to standart trajectory finish point
            tempEndPoint.x += randomOffsetEndPoint.x;
            tempEndPoint.z += randomOffsetEndPoint.y;

            // Creating fly path
            var arrowTweenPath = new Vector3[2];
            arrowTweenPath[0] = Vector3.Lerp(startPoint, tempEndPoint, 0.5f);
            arrowTweenPath[0].y += height;
            arrowTweenPath[1] = tempEndPoint;

            ShootOneArrow(flyDuration, startPoint, arrowTweenPath);
            yield return new WaitForSeconds(Random.Range(0, kShootPauseMin));
        }
    }

    #endregion

    #region Private methods

    private void OnArrowFlyFinish()
    {
        _arrowsInTheAir--;
        MyDebug.LogGray("[ShootingController] Arrows in the air: " + _arrowsInTheAir);
        if (_arrowsInTheAir <= 0)
        {
            _onAllArrowsFinished?.Invoke();
        }
    }
    
    private void CreateNewArrows(int arrowsNumber, Side side)
    {
        for (int i = 0; i < arrowsNumber; i++)
        {
            var arrow = CreateAndAddNewArrowToStack(side);
            if (i == arrowsNumber -1)
                MainGameManager.Instance.SwitchArrowsCameraToArrow(arrow.transform);
        }
    }

    private void ShootOneArrow(float standartFlyDuration, Vector3 startPoint, Vector3[] arrowTweenPath)
    {
        try
        {
            var arrowCtrl = _currentArrowCtrls.Pop();
            arrowCtrl.transform.position = startPoint;
            arrowCtrl.transform.SetParent(null);
            arrowCtrl.Fly(arrowTweenPath, standartFlyDuration);
            
            if(Random.value < kArrowSoundChance)
                SoundManager.Instance.PlayArrowSound();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private GameObject CreateAndAddNewArrowToStack(Side side)
    {
        var arrow = MasterObjectPooler.Instance.GetObject("Arrows");
        //arrow.transform.SetParent(side == Side.Player ? _playersArrowStartPoint : _enemiesArrowStartPoint);
        /*arrow.transform.localPosition = Vector3.zero;
        arrow.transform.localRotation = Quaternion.identity;*/
        arrow.transform.position = side == Side.Player ? _playersArrowStartPoint : _enemiesArrowStartPoint;
        var ctrl = arrow.GetComponent<ArrowController>();
        ctrl.arrowBelonging = side == Side.Player ? ArrowController.ArrowBelonging.Player : ArrowController.ArrowBelonging.Enemy;
        ctrl.OnFlyFinish = OnArrowFlyFinish;
        _currentArrowCtrls.Push(ctrl);
        
        return arrow;
    }

    /*private IEnumerator LineShootNowForPlayerCoroutine(float standartFlyDuration, Vector3 leftPoint, float abMagnitude, Vector3 abDirection, Vector3[] path)
    {
        Vector3 GetNewRandomPointOnLine()
        {
            var newLen = abMagnitude * Random.value;
            return leftPoint + abDirection * newLen;
        }

        var lastPath = _drawTrajectory.GetCurrentPath();

        const float kRangeSpreading = 1f;

        while (_currentArrowCtrls.Count != 0)
        {
            var startPoint = GetStartPointInRadius(_playersArrowStartPoint, kArrowAppearRadius);
            var tempEndPoint = GetNewRandomPointOnLine();
            var randomOffsetEndPoint =
                Random.insideUnitCircle * kRangeSpreading; // final offset to standart trajectory finish point
            tempEndPoint.x += randomOffsetEndPoint.x;
            tempEndPoint.z += randomOffsetEndPoint.y;

            var arrowTweenPath = new Vector3[2];
            arrowTweenPath[0] = Vector3.Lerp(startPoint, tempEndPoint, 0.5f);
            arrowTweenPath[0].y += lastPath.Item2;
            arrowTweenPath[1] = tempEndPoint;

            ShootOneArrow(standartFlyDuration, startPoint, arrowTweenPath);
            yield return new WaitForSeconds(Random.Range(0, kShootPauseMin));
        }
    }*/

    private Vector3 GetStartPointInRadius(Vector3 basicStartPoint, float radius)
    {
        return basicStartPoint + Random.insideUnitSphere * radius;
    }
    
    private Vector3 GetCenterOfTargets(IReadOnlyList<Transform> targets)
    {
        var bound = new Bounds(targets[0].position, Vector3.zero);
        
        for (int i = 1; i < targets.Count; i++)
            bound.Encapsulate(targets[i].position);

        return bound.center;
    }
    
    #endregion
}
