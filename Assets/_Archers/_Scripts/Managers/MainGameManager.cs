using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public enum GameState
{
    LoadingLevel,
    UpgradingUnits,
    MovingToNextPlacement,
    WaitingTouch,
    Preparing,
    Targeting,
    Shooting,
    WaitingShootBack
}

public partial class MainGameManager : HMSingleton<MainGameManager>
{
    #region Constants

    private const int kLevelsNumber = 1; 
    
    const float kVerticalStartPointOffset = 1f;
    const float kHorizontalStartPointOffsetForPlayers = 2f;
    const float kHorizontalStartPointOffsetForEnemies = 1f;

    private const float kPlayersArrowsSpreading = 4f;
    private const float kEnemyArrowsSpreading = 6f;
    
    private const float kPauseBeforeShootBack = 0.2f;
    private const int   kStartPlayerArchersDebugNumber = 1;
    private const float kPlayerArchersGroupMinRadius = 0;
    private const float kPlayerArchersGroupMaxRadius = 10; // Для изменения зума камеры при натягивании лука
    
    private const float kArrowsMaxFlyDuration = 4.0f;
    private const float kArrowsMinFlyDuration = 0.8f;

    #endregion

    #region Properties

    private float CurrentEnemiesSpread => kEnemyArrowsSpreading; // TODO: Calculate 
    [ShowNativeProperty] public GameState GameState { get; private set; } = GameState.LoadingLevel;

    #endregion

    #region Events

    public static event Action OnNeedToRemoveOldArrows;
    public GameData gameData;

    #endregion
    
    #region Private vars
    
    private List<PlayerArcherController> _playerArchers;
    private List<PlayerArcherController> _deadPlayerArchers;

    private LevelController              _currentLevelController;
    private float                        _currentShootPowerNormalized; // Нормализованое значение силы выстрела
    private float                        _currentShootAngleNormalized; // Нормализованное значение угла выстрела
    private Vector3                      _prevMousePos; // точка начала тача при прицеливании
    private PositionsPlacementController _currentPlayerArchersPlacement;
    private float                        _currentRealFlyDuration;
    private List<EnemyPositionData>      _enemyPositionDatas;
    private int                          _currentEnemyPlacementIndex;
    private int                          _currentPlayerPlacementIndex;
    private int                          _playerArchersGoing; // Сколько сейчас бежит лучников, чтобы корректно завершить перебежку
    private Vector3                      _currentHorizontalDirectionToEnemies;
    private Vector3                      _currentPlayersMiddlePoint;

    [SerializeField] private UIManager                _uiManager;
    [SerializeField] private UnitsUpgradeController   _unitsUpgradeController;
    [SerializeField] private ShootingController       _shootingController;
    [SerializeField] private CinemachineVirtualCamera _targetingCamera; // во время приготовления
    [SerializeField] private CinemachineVirtualCamera _backRunningCamera; // во время бега
    [SerializeField] private CinemachineVirtualCamera _arrowsCamera; // во время полета стрел
    [SerializeField] private CinemachineVirtualCamera _allViewCamera; // На старте уровня
    [SerializeField] private CinemachineVirtualCamera _enemiesShootCamera; // При выстреле врага
    [SerializeField] private CinemachineVirtualCamera _unitsUpgradeCamera; // При апгрейде юнитов над ними
    [SerializeField] private DollyTrackController     _dollyTrackController;
    
    [Space]
    [SerializeField] private CinemachineTargetGroup _playersTargetGroup;
    [SerializeField] private CinemachineTargetGroup _enemiesTargetGroup;
    //[SerializeField] private CinemachineTargetGroup _arrowsTargetGroup;
    [SerializeField] private GameObject             _playerArcherPrefab;
    [SerializeField] private GameObject             _enemyArcherPrefab;
    [SerializeField] private DrawTrajectory         _drawTrajectory;

    #endregion

    #region Init

    private void Start()
    {
        StartCoroutine(StartNewLevel());
    }

    #endregion

    #region Update

    private void Update()
    {
        ProcessTouch();
    }

    #endregion

    #region Cameras

    public void SwitchToAllViewCamera(Transform viewObject)
    {
        _allViewCamera.Follow = viewObject;
        _allViewCamera.LookAt = viewObject;
        
        _backRunningCamera.Priority = 10;
        _arrowsCamera.Priority = 10;
        _targetingCamera.Priority = 10;
        _allViewCamera.Priority = 20;
        _enemiesShootCamera.Priority = 10;
        _unitsUpgradeCamera.Priority = 10;
    }
        
    public void SwitchToPlayersTargetingCamera()
    {
        _backRunningCamera.Priority = 10;
        _arrowsCamera.Priority = 10;
        _targetingCamera.Priority = 20;
        _allViewCamera.Priority = 10;
        _enemiesShootCamera.Priority = 10;
        _unitsUpgradeCamera.Priority = 10;
    }
    
    public void SwitchToArrowsCamera()
    {
        _backRunningCamera.Priority = 10;
        _targetingCamera.Priority = 10;
        _arrowsCamera.Priority = 20;
        _allViewCamera.Priority = 10;
        _enemiesShootCamera.Priority = 10;
        _unitsUpgradeCamera.Priority = 10;
    }

    public void SwitchToPlayersBackCamera(bool lookToEnemies = false)
    {
        _backRunningCamera.LookAt = lookToEnemies ? _enemiesTargetGroup.transform : _playersTargetGroup.transform;
        _backRunningCamera.Priority = 20;
        _targetingCamera.Priority = 10;
        _arrowsCamera.Priority = 10;
        _allViewCamera.Priority = 10;
        _enemiesShootCamera.Priority = 10;
        _unitsUpgradeCamera.Priority = 10;
    }

    public void SwitchToEnemiesShootCamera()
    {
        _backRunningCamera.Priority = 10;
        _targetingCamera.Priority = 10;
        _arrowsCamera.Priority = 10;
        _allViewCamera.Priority = 10;
        _enemiesShootCamera.Priority = 20;
        _unitsUpgradeCamera.Priority = 10;
    }

    public void SwitchToUnitsUpgradeCamera()
    {
        _backRunningCamera.Priority = 10;
        _targetingCamera.Priority = 10;
        _arrowsCamera.Priority = 10;
        _allViewCamera.Priority = 10;
        _enemiesShootCamera.Priority = 10;

        _unitsUpgradeCamera.m_Follow = _currentPlayerArchersPlacement.transform;
        _unitsUpgradeCamera.m_LookAt = _currentPlayerArchersPlacement.transform;
        _unitsUpgradeCamera.Priority = 20;
    }

    private void SetSideCameraRadiusFactor(float factor)
    {
        for (int i = 0; i < _playersTargetGroup.m_Targets.Length; i++)
        {
            _playersTargetGroup.m_Targets[i].radius = factor;
        }
    }
    
    #endregion

    #region Archers placement

    private void PositionPlayerArchersAtLevelStart()
    {
        ClearOldArchers();

        _currentPlayerArchersPlacement = _currentLevelController.GetStartPlayerPlacement();
        /*for (int i = 0; i < kStartPlayerArchersDebugNumber; i++)
        {
            CreateNewPlayerArcher(i);
        }*/

        GetUnitsFromSave();
    }

    private void CreateNewPlayerArcher(int positionInPlacement)
    {
        var archer = Instantiate(_playerArcherPrefab);
        var archerController = archer.GetComponent<PlayerArcherController>();
        archerController.onRunFinish = OnPlayerArchersFinishMoving;
        archerController.onDeath = OnPlayerUnitDead;
        archerController.positionInPlacement = positionInPlacement;
        archer.transform.position = _currentPlayerArchersPlacement.GetPositionFor(positionInPlacement);
        _playerArchers.Add(archerController);
        archerController.SwitchToMiddleTilt();
    }

    private void ClearOldArchers()
    {
        if (_deadPlayerArchers != null)
        {
            for (int i = 0; i < _deadPlayerArchers.Count; i++)
                if(_deadPlayerArchers[i] != null)
                    Destroy(_deadPlayerArchers[i].gameObject);

            _deadPlayerArchers.Clear();
        }
        else
            _deadPlayerArchers = new List<PlayerArcherController>();

        if (_playerArchers != null)
        {
            for (int i = 0; i < _playerArchers.Count; i++)
            {
                if (_playerArchers[i])
                    Destroy(_playerArchers[i].gameObject);
            }

            _playerArchers.Clear();
        }
        else
            _playerArchers = new List<PlayerArcherController>();
    }

    private void PositionEnemyArchersAtLevelStart()
    {
        var enemyPlacements = _currentLevelController.GetAllEnemyPlacements();
        for (int i = 0; i < enemyPlacements.Count; i++)
        {
            var newEnemyPositionData = new EnemyPositionData(_currentLevelController.GetEnemyPlacement(i).GetStartEnemies(),
                _enemyArcherPrefab, enemyPlacements[i], i, OnEnemyPlacementCleared);
            _enemyPositionDatas.Add(newEnemyPositionData);
        }
    }

    private int GetFirstFreePositionInPlacement()
    {
        var set = new HashSet<int>();
        for (int i = 0; i < _playerArchers.Count(); i++)
            set.Add(_playerArchers[i].positionInPlacement);

        for (int i = 0; i < _currentPlayerArchersPlacement.GetPositionsNumber(); i++)
        {
            if (!set.Contains(i))
                return i;
        }

        return -1; // All positions occupied
    }
    
    #endregion

    #region SaveLoad

    private void GetUnitsFromSave()
    {
        int[] unitsArray = StatePersister.Instance.LoadUnitPosition();
        
        for (int i = 0; i < unitsArray.Length; i++)
        {
            if (unitsArray[i] >= 0)
            {
                LoadUnitWithPos(i,unitsArray[i]);
            }
        }
    }

    #endregion

    #region Private methods

    private IEnumerator StartNewLevel()
    {
        GameState = GameState.LoadingLevel;
        _uiManager.SetLevelNumber(StatePersister.Instance.CurrentLevel);

        //ClearOldLevel();

        yield return null;
        
        _currentEnemyPlacementIndex = -1;
        _currentPlayerPlacementIndex = -1; // starting with buying and upgrading
        //Расставление и объединение юнитов игрока там же
        LoadLevelScene();
        _uiManager.UnHideScreen(UpgradeUnitsState); // Next will wait for the Play button press
    }

    private void ClearOldLevel()
    {
        OnNeedToRemoveOldArrows?.Invoke();
        if (_currentLevelController != null)
        {
            for (int i = 0; i < _playerArchers.Count; i++)
            {
                Destroy(_playerArchers[i].gameObject);
            }

            for (int i = 0; i < _enemyPositionDatas.Count; i++)
            {
                _enemyPositionDatas[i].ClearArchers();
            }

            Destroy(_currentLevelController.gameObject);
        }
    }

    private void LoadLevelScene()
    {
        //var levelIndex = StatePersister.Instance.CurrentLevel;
        //SceneManager.LoadScene("Level" + levelIndex);
        SceneManager.LoadScene(2);
        
        _enemyPositionDatas?.Clear();
        _enemyPositionDatas = new List<EnemyPositionData>();
        
        // Дальше ждем сработки SetNewLevelController после загрузки уровня
    }

    // Выполняется после уничтожения противника на очередном раунде и после покупки новых лучников и перебежки
    private void PrepareForNewShootingRound()
    {
        _currentEnemyPlacementIndex++;
        InitEnemiesCinemachineGroup();
        RotatePlayersToNewTargetPoint();
        RotateEnemiesToNewTargetPoint();
        
        _currentPlayerArchersPlacement = _currentLevelController.GetPlayerPlacement(_currentPlayerPlacementIndex);
        UpdateDirectionToEnemies();
        _shootingController.SetArrowsAppearPoints(GetTrajectoryStartPointForEnemyArchers(), 
            GetTrajectoryStartPointForPlayerArchers());
        _dollyTrackController.SetNewPlacements(_currentPlayerArchersPlacement.transform, 
            _currentLevelController.GetEnemyPlacement(_currentEnemyPlacementIndex).transform);
    }

    private void UpgradeUnitsState()
    {
        GameState = GameState.UpgradingUnits;
        SwitchToUnitsUpgradeCamera();
        _unitsUpgradeController.SetCurrentPlacement(_currentPlayerArchersPlacement);
        _currentPlayerArchersPlacement.ShowPositions();

        //DOVirtual.DelayedCall(2f, _uiManager.ShowNewRoundButtonsBlock, false);
        _uiManager.ShowNewRoundButtonsBlock();
    }

    private IEnumerator PlayNow()
    {
        SwitchToPlayersBackCamera(true); // At the round begining we should look to enemies

        yield return new WaitForSeconds(2f);
        
        SwitchToPlayersTargetingCamera();
        
        yield return new WaitForSeconds(0.5f);
        GameState = GameState.WaitingTouch;
    }
    
    private void InitPlayersCinemachineGroup()
    {
        _playersTargetGroup.m_Targets = new CinemachineTargetGroup.Target[_playerArchers.Count];
        for (int i = 0; i < _playerArchers.Count; i++)
        {
            var target = new CinemachineTargetGroup.Target
            {
                target = _playerArchers[i].transform,
                weight = 1,
                radius = 0
            };
            _playersTargetGroup.m_Targets[i] = target;
        }
    }
    
    private void InitEnemiesCinemachineGroup()
    {
        var enemies = _enemyPositionDatas[_currentEnemyPlacementIndex].GetAliveEnemiesTransforms();
        _enemiesTargetGroup.m_Targets = new CinemachineTargetGroup.Target[enemies.Count];
        
        for (int i = 0; i < enemies.Count; i++)
        {
            var target = new CinemachineTargetGroup.Target
            {
                target = enemies[i],
                weight = 1,
                radius = 0
            };
            _enemiesTargetGroup.m_Targets[i] = target;
        }
        
        _enemiesTargetGroup.DoUpdate();
        
        _enemiesTargetGroup.transform.LookAt(_playersTargetGroup.transform, Vector3.up);
        var newRot = _enemiesTargetGroup.transform.localRotation.eulerAngles;
        newRot.x = 0;
        newRot.z = 0;
        _enemiesTargetGroup.transform.localRotation = Quaternion.Euler(newRot);
    }

    private void PrepareForShootPlayerArchers()
    {
        GameState = GameState.Preparing;
        _shootingController.Init();
        SwitchToPlayersTargetingCamera();
        
        _currentPlayersMiddlePoint = GetCenterOfAlivePlayerArchers();
        UpdateDirectionToEnemies();

        for (int i = 0; i < _playerArchers.Count; i++)
            _playerArchers[i].AnimatePrepareForShooting();
        
        GameState = GameState.Targeting;
    }

    private void UpdateDirectionToEnemies()
    {
        _currentHorizontalDirectionToEnemies =
            (GetCenterOfAliveEnemies() - _currentPlayerArchersPlacement.transform.position).normalized;
        _currentHorizontalDirectionToEnemies = Vector3.ProjectOnPlane(_currentHorizontalDirectionToEnemies, Vector3.up);
    }

    private IEnumerator StartPlayerShoot()
    {
        GameState = GameState.Shooting;
        
        var allArrowsFinished = false;
        var targets = FindTargetsOnTrajectory();
        SetSideCameraRadiusFactor(0); // reset
        
        for (int i = 0; i < _playerArchers.Count; i++)
            _playerArchers[i].AnimateShooting(_currentShootPowerNormalized);

        var (pos, procent) = _drawTrajectory.GetPosWithSignChanged();
        var arrowsFlyDuration = GetArrowsFlyTime((GetCenterOfAlivePlayerArchers() - pos).magnitude * (1f/procent));

        if (targets.Count == 0) // missed by pre-calculation 
        {
            MyDebug.LogBlue("[MGM] No targets found by trajectory, performing basic shoot");
            // Если не попадаем, то цели просто летят и падают линией, распределение овалом, минимально по дальности, максимально по ширине

            // debug only
            int arrows = 0;
            for (int i = 0; i < _playerArchers.Count; i++)
                arrows += _playerArchers[i].GetArrowsNumber();

            //_shootingController.LineShootNow(Side.Player, arrows, arrowsFlyDuration, leftEdgePoint, abMagnitude, abDirection);
            _shootingController.ShootNowForPlayerWithDiffAngle(arrows, arrowsFlyDuration, _currentHorizontalDirectionToEnemies, _currentShootPowerNormalized, _currentShootAngleNormalized, () => allArrowsFinished = true);
        }
        else // Should hit targets
        {
            // TODO: Не закончено
            // Каждый лучник стреляет своей группой стрел
            // Мы получаем список групп в виде листа
            // Каждой группе назначается цель из найденных
            // Каждой стреле надо еще дать вероятность процентов в 80, чтобы был шанс промахнуться
            // Причем цели надо давать с вероятностью, даже если стрел много, должна быть вероятность промахнуться, даже если враги на одной линии
            MyDebug.LogBlue("[MGM] " + targets.Count + " targets found by trajectory, performing targeted shoot");
            var groups = GetPlayerArrowsGroups();
            _shootingController.GroupShootForPlayers(groups, targets, arrowsFlyDuration, _currentShootPowerNormalized, _currentShootAngleNormalized, () => allArrowsFinished = true);
        }
        
        SwitchToArrowsCamera();
        _drawTrajectory.MakeCurrentsDotsAsPrevsAndHide();

        yield return new WaitUntil(() => allArrowsFinished);
        yield return new WaitForSeconds(kPauseBeforeShootBack);

        if (GameState == GameState.Shooting) // Если стейт другой - значит все враги погибли при прошлом выстреле
        {
            ResetPlayerArchersTilt();
            StartCoroutine(StartEnemyShoot());
        }
    }

    private IEnumerator StartEnemyShoot()
    {
        const float kEnemyArchersShootHeight = 5f;
        const float kEnemyPrepareTime = 1.0f;
        
        var allArrowsFinished = false;
        GameState = GameState.WaitingShootBack;
        
        _shootingController.Init();
        InitEnemiesCinemachineGroup();
        SwitchToEnemiesShootCamera();
        
        _enemyPositionDatas[_currentEnemyPlacementIndex].PrepareForShoot();
        yield return new WaitForSeconds(kEnemyPrepareTime);
        _enemyPositionDatas[_currentEnemyPlacementIndex].AnimateShooting();

        var arrowsFlyDuration = GetArrowsFlyTime((GetCenterOfAlivePlayerArchers() - GetCenterOfAliveEnemies()).magnitude);
        // TODO: Тут надо в зависимости от сложности крутить вероятность попадания через спреад и сдвиг +- дальности
        _shootingController.BasicShootWithCircleSpreadingForEnemies(_enemyPositionDatas[_currentEnemyPlacementIndex].GetArrowsNumber(), _currentPlayersMiddlePoint, kEnemyArchersShootHeight, arrowsFlyDuration, CurrentEnemiesSpread, () => allArrowsFinished = true); 
        SwitchToArrowsCamera();

        yield return new WaitUntil(() => allArrowsFinished);
        yield return new WaitForSeconds(kPauseBeforeShootBack);

        if (GameState == GameState.WaitingShootBack)
        {
            // Выжили
            SwitchToPlayersTargetingCamera();
            _drawTrajectory.ShowPrevDots();
            GameState = GameState.WaitingTouch;
        }
    }

    private List<int> GetPlayerArrowsGroups()
    {
        // Arrows power not counted now
        var arrowGroups = new List<int>();
        for (int i = 0; i < _playerArchers.Count; i++)
        {
            if(!_playerArchers[i].IsDead)
                arrowGroups.Add(_playerArchers[i].GetArrowsNumber());
        }
        return arrowGroups;
    }
    private void RotatePlayersToNewTargetPoint()
    {
        var targetPoint = _currentLevelController.GetEnemyPlacementPosition(_currentEnemyPlacementIndex);
        RotatePlayersTo(targetPoint);
    }

    private void RotatePlayersTo(Vector3 targetPoint)
    {
        for (int i = 0; i < _playerArchers.Count; i++)
            _playerArchers[i].LookTo(targetPoint);
        
        _playersTargetGroup.DoUpdate();
        _playersTargetGroup.transform.LookAt(targetPoint, Vector3.up);
        var newRot = _playersTargetGroup.transform.localRotation.eulerAngles;
        newRot.x = 0;
        newRot.z = 0;
        _playersTargetGroup.transform.localRotation = Quaternion.Euler(newRot); 
    }
    
    private void RotateEnemiesToNewTargetPoint()
    {
        var targetPoint = _currentLevelController.GetPlayerPlacement(_currentPlayerPlacementIndex).transform.position;
        for (int i = 0; i < _enemyPositionDatas.Count; i++)
        {
            _enemyPositionDatas[i].RotateEnemiesTo(targetPoint);
        }
    }

    private void ProcessTouch()
    {
        const float kDeltaToNormFactor = 0.003f; // Коэф преобразования сдвига тача к нормализованным мощности и углу
        
        if (GameState == GameState.WaitingTouch && Input.GetMouseButtonDown(0))
        {
            _prevMousePos = Input.mousePosition;
            _currentShootAngleNormalized = Mathf.InverseLerp(_drawTrajectory.kMinHorizonAngle, _drawTrajectory.kMaxHorizonAngle, 30);
            _currentShootPowerNormalized = 0.5f;
            PrepareForShootPlayerArchers();
            UpdateTrajectory();
            _uiManager.ShowPowerTextBlock();
        }

        if (GameState == GameState.Targeting && Input.GetMouseButtonUp(0))
        {
            _uiManager.HidePowerTextBlock();
            StartCoroutine(StartPlayerShoot());

            ResetPlayerArchersTilt();
        }

        if (GameState == GameState.Targeting && Input.GetMouseButton(0))
        {
            // Идет прицеливание
            var mousePos = Input.mousePosition;
            var delta = mousePos - _prevMousePos;
            delta.y = -delta.y; // we invert Y becouse swipe down must increase angle
            if (delta != Vector3.zero)
            {
                _currentShootPowerNormalized = Mathf.Clamp01(_currentShootPowerNormalized + delta.x * kDeltaToNormFactor);
                _currentShootAngleNormalized = Mathf.Clamp01(_currentShootAngleNormalized + delta.y * kDeltaToNormFactor);

                const float kMinShowPower = 0.15f;
                var powerToShow = _currentShootPowerNormalized * (1 - kMinShowPower) + kMinShowPower;
                _uiManager.SetAngleAndPower(Mathf.RoundToInt(Mathf.Lerp(_drawTrajectory.kMinHorizonAngle, _drawTrajectory.kMaxHorizonAngle, _currentShootAngleNormalized)), powerToShow);
                
                _prevMousePos = mousePos;
                UpdateTrajectory();
            }
        }
    }

    private void ResetPlayerArchersTilt()
    {
        for (int i = 0; i < _playerArchers.Count; i++)
            _playerArchers[i].SetTilt(0);
    }

    private Vector3 GetCenterOfAliveEnemies()
    {
        var enemies = _enemyPositionDatas[_currentEnemyPlacementIndex].GetAliveEnemiesTransforms();
        var bound = new Bounds(enemies[0].position, Vector3.zero);
        
        for (int i = 1; i < enemies.Count(); i++)
            bound.Encapsulate(enemies[i].position);

        return bound.center;
    }

    private Vector3 GetCenterOfAlivePlayerArchers()
    {
        var bound = new Bounds(_playerArchers[0].transform.position, Vector3.zero);
        
        for (int i = 1; i < _playerArchers.Count; i++)
            bound.Encapsulate(_playerArchers[i].transform.position);

        return bound.center;
    }

    private void MovePlayerArchersToNextPlacement()
    {
        MyDebug.LogYellow("[MGM] Moving archers");

        InitPlayersCinemachineGroup();
        // Побежать до следующей точки
        GameState = GameState.MovingToNextPlacement;
        _playerArchersGoing = _playerArchers.Count;
        var newPlacement = _currentLevelController.GetPlayerPlacement(_currentPlayerPlacementIndex);
        RotatePlayersTo(newPlacement.transform.position);
        SwitchToPlayersBackCamera();
        for (int i = 0; i < _playerArchers.Count; i++)
        {
            var archer = _playerArchers[i];
            var newPos = newPlacement.GetPositionFor(archer.positionInPlacement);
            archer.RunToPoint(newPos);
        }
        
        SoundManager.Instance.PlayRunSound();
    }

    #endregion

    #region Trajectory

    private void UpdateTrajectory()
    {
        for (int i = 0; i < _playerArchers.Count; i++)
            _playerArchers[i].SetTilt(_currentShootAngleNormalized, _currentShootPowerNormalized);

        DrawTrajectory();
        SetSideCameraRadiusFactor(Mathf.Lerp(kPlayerArchersGroupMinRadius, kPlayerArchersGroupMaxRadius, _currentShootPowerNormalized));
    }

    private void DrawTrajectory()
    {
        var tempStartPoint = GetTrajectoryStartPointForPlayerArchers();
        _drawTrajectory.CalculateAndDrawDotsTrajectory(tempStartPoint, _currentHorizontalDirectionToEnemies, _currentShootPowerNormalized, _currentShootAngleNormalized);
    }

    private Vector3 GetTrajectoryStartPointForPlayerArchers()
    {
        return _currentPlayerArchersPlacement.transform.position + Vector3.up * kVerticalStartPointOffset + _currentHorizontalDirectionToEnemies * kHorizontalStartPointOffsetForPlayers;
    }

    private Vector3 GetTrajectoryStartPointForEnemyArchers()
    {
        return GetCenterOfAliveEnemies() + Vector3.up * kVerticalStartPointOffset + (-_currentHorizontalDirectionToEnemies) * kHorizontalStartPointOffsetForEnemies;
    }
        
    private List<Transform> FindTargetsOnTrajectory()
    {
        Vector3 RotateTowardsUp(Vector3 normalizedStart, float angle)
        {
            var q = Quaternion.AngleAxis(angle, Vector3.up);
            return q * normalizedStart;
        }
        
        bool IsDead(Transform unitTr)
        {
            var c = unitTr.GetComponent<EnemyArcherController>();
            return c == null || c.IsDead;
        }
        
        const float kMaxCastDistance = 6f;
        var results = new RaycastHit[10];
        
        var startPoint = GetTrajectoryStartPointForPlayerArchers();
        var path = _drawTrajectory.GetCurrentPath();
        var targets = new List<Transform>();
        
        // Получаем два вектора от конечной точки, это будут направления каста
        var a = startPoint - path[path.Length-1];
        a.y = 0;
        a.Normalize();
        var castDirectionRight = RotateTowardsUp(a, 90);
        castDirectionRight *= kPlayersArrowsSpreading;
        var castDirectionLeft = -castDirectionRight;

        var layer = LayerMask.NameToLayer("Unit");
        // Пробегаемся по точкам пути
        // От каждой точки кастуем в две стороны лучи и проверяем, поподают ли в них враги
        // Если попадают, то собираем список местонахождений врагов и возвращаем их
        for (int i = 0; i < path.Length; i++)
        {
            var pointToCastFromLeft = path[i] + castDirectionLeft;
            
            var leftResultsNumber =
                Physics.RaycastNonAlloc(pointToCastFromLeft, -castDirectionLeft, results, kMaxCastDistance, 1 << layer);
            Debug.DrawRay(pointToCastFromLeft, -castDirectionLeft, Color.blue, 1.5f);
            if(leftResultsNumber > 0)
                for (int j = 0; j < leftResultsNumber; j++)
                {
                    var tr = results[j].transform;
                    if(!targets.Contains(tr) && tr.CompareTag("Enemy"))
                        targets.Add(tr);
                }
            
            var pointToCastFromRight = path[i] + castDirectionRight;
            var rightResultsNumber =
                Physics.RaycastNonAlloc(pointToCastFromRight, -castDirectionRight, results, kMaxCastDistance, 1 << layer);
            Debug.DrawRay(pointToCastFromRight, -castDirectionRight, Color.red, 1.5f);
            if(rightResultsNumber > 0)
                for (int j = 0; j < rightResultsNumber; j++)
                {
                    var tr = results[j].transform;
                    if(!targets.Contains(tr) && tr.CompareTag("Enemy"))
                        targets.Add(tr);
                }
        }

        //var endPointLeft = path[path.Length-1] + castDirectionLeft;
        //var endPointRight = path[path.Length-1] + castDirectionRight;

        targets.RemoveAll(IsDead);
        
        return targets;
    }

    #endregion
    
    #region Debug
    

    #endregion

    #region Delegate

    private void OnPlayerUnitDead(UnitController playerCtrl)
    {
        _playerArchers.Remove(playerCtrl as PlayerArcherController);
        _deadPlayerArchers.Add(playerCtrl as PlayerArcherController);

        if (_playerArchers.Count == 0)
        {
            GameState = GameState.LoadingLevel;
            _uiManager.HideScreen(() => StartCoroutine(StartNewLevel()));
            
            // TODO: Implement level lost
        }
    }
    
    private void OnEnemyPlacementCleared(int enemyPlacementIndex)
    {
        MyDebug.LogBlue("[MGM] Enemy placement cleared! - " + enemyPlacementIndex);

        if (_currentEnemyPlacementIndex < _enemyPositionDatas.Count - 1)
        {
            // There is one more round
            SoundManager.Instance.PlayWinRoundSound();
            for (int i = 0; i < _playerArchers.Count; i++)
            {
                _playerArchers[i].Cheer();
            }
            
            UpgradeUnitsState();
        }
        else
        {
            // Level cleared
            if (StatePersister.Instance.CurrentLevel < kLevelsNumber)
                StatePersister.Instance.CurrentLevel++;
            else
                StatePersister.Instance.CurrentLevel = Random.Range(0, kLevelsNumber); // TODO: Тут потом не зацикливать, а делать рандом
            
            //add coins stuff and multiply new reward
            _uiManager.AddCoins(StatePersister.Instance.AwardForLevelFinish);
            var newCoins = StatePersister.Instance.AwardForLevelFinish * gameData.coinsAwardMultiplier;
            StatePersister.Instance.AwardForLevelFinish = (int)newCoins;
            
            StartCoroutine(StartNewLevel());
        }
    }

    private void OnPlayerArchersFinishMoving()
    {
        _playerArchersGoing--;
        
        if (_playerArchersGoing == 0) // All archers reached their destination
        {
            SoundManager.Instance.StopRunSound();
            PrepareForNewShootingRound();
            StartCoroutine(PlayNow());
        }
    }

    #endregion
    
    #region Public methods

    public void SetNewLevelController(LevelController levelController)
    {
        _currentLevelController = levelController;
        
        PositionPlayerArchersAtLevelStart();
        PositionEnemyArchersAtLevelStart();
        SwitchToAllViewCamera(_currentPlayerArchersPlacement.transform);
    }
    
    public void PlayButtonPressed()
    {
        _currentPlayerPlacementIndex++;
        _currentPlayerArchersPlacement.HidePositions();
        MovePlayerArchersToNextPlacement();
    }

    public void BuyUnit()
    {
        _uiManager.AddCoins(-gameData.startUnitCost);
        
        var newPos = GetFirstFreePositionInPlacement();
        CreateNewPlayerArcher(newPos);
        if(newPos >= 0)
            StatePersister.Instance.SaveUnitPosition(newPos, 0); // save position for new unit
        gameData.buysCount++; // increase multiplier count

        SoundManager.Instance.PlayBubbleSound();
        MyDebug.LogYellow("[MGM] New archer bought");

    }
    
    public void LoadUnitWithPos(int _position, int _unitLevel)
    {
        var archer = Instantiate(_playerArcherPrefab);
        var archerController = archer.GetComponent<PlayerArcherController>();
        archerController.onRunFinish = OnPlayerArchersFinishMoving;
        archerController.onDeath = OnPlayerUnitDead;
        archerController.positionInPlacement = _position;
        archerController.SetUnitLevel(_unitLevel);
        archer.transform.position = _currentPlayerArchersPlacement.GetPositionFor(_position);
        _playerArchers.Add(archerController);
        archerController.SwitchToMiddleTilt();
        
        MyDebug.LogYellow("[MGM] New archer loaded");
    }

    public bool CanBuyUnits()
    {
        return _currentPlayerArchersPlacement.GetPositionsNumber() > _playerArchers.Count &&
               StatePersister.Instance.PlayerCoins >= gameData.startUnitCost;
    }

    public void UpgradeUnit(PlayerArcherController unitToUpgrade, PlayerArcherController unitToRemove)
    {
        unitToUpgrade.UpUnitLevel();
        SoundManager.Instance.PlayBubbleSound();
        unitToUpgrade.transform.DOScale(1f, 0.2f).From(1.3f);
        _playerArchers.Remove(unitToRemove);
        Destroy(unitToRemove.gameObject);
        _uiManager.UpdateBuyButtonState();
    }

    public (bool, PlayerArcherController) IsPositionInPlacementOccupied(int pos)
    {
        var occupied = false;
        PlayerArcherController toUpgrade = null;
        
        for (int i = 0; i < _playerArchers.Count; i++)
        {
            if (_playerArchers[i].positionInPlacement == pos)
            {
                occupied = true;
                toUpgrade = _playerArchers[i];
                break;
            }
        }

        return (occupied, toUpgrade);
    }
    
    public void SwitchArrowsCameraToArrow(Transform arrow)
    {
        _arrowsCamera.m_LookAt = arrow;
        _arrowsCamera.m_Follow = arrow;
    }

    public void KillAllEnemies()
    {
        _enemyPositionDatas[_currentEnemyPlacementIndex].KillEnemies();
    }

    public float GetArrowsFlyTime(float flyLength)
    {
        const float kMinLenght = 2f;
        const float kMaxLenght = 28f;
        var proc = Mathf.InverseLerp(kMinLenght, kMaxLenght, flyLength);
        var time = Mathf.Lerp(kArrowsMinFlyDuration, kArrowsMaxFlyDuration, proc);
        MyDebug.LogGray("[MGM] Fly time: " + time);
        return time;
    }

    #endregion
}
