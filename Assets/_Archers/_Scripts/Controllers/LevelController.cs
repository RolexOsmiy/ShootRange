using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [ReadOnly][SerializeField] private Transform _startPlayersPosition;
    [ReadOnly][SerializeField] private Transform _enemyPositionsContainer;
    [ReadOnly][SerializeField] private Transform _playerPositionsContainer;
    [ReadOnly][SerializeField] private Transform[] _enemyPositions;
    [ReadOnly][SerializeField] private Transform[] _playerPositions;

    #region Init

    private void Awake()
    {
        MainGameManager.Instance.SetNewLevelController(this);
    }

    #endregion
    
    public Vector3 GetEnemyPlacementPosition(int placementIndex)
    {
        return _enemyPositions[placementIndex].position;
    }
   
    public PositionsPlacementController GetStartPlayerPlacement()
    {
        return _startPlayersPosition.GetComponent<PositionsPlacementController>();
    }
    
    public PositionsPlacementController GetPlayerPlacement(int placementIndex)
    {
        return _playerPositions[placementIndex].GetComponent<PositionsPlacementController>();
    }
    
    public PositionsPlacementController GetEnemyPlacement(int placementIndex)
    {
        return _enemyPositions[placementIndex].GetComponent<PositionsPlacementController>();
    }
    
    // Для того, чтобы из MGM выставить всех врагов
    public List<PositionsPlacementController> GetAllEnemyPlacements()
    {
        var l = new List<PositionsPlacementController>();
        for (int i = 0; i < _enemyPositions.Length; i++)
        {
            l.Add(_enemyPositions[i].GetComponent<PositionsPlacementController>());
        }

        return l;
    }

    #region Utils

    [Button(null, EButtonEnableMode.Editor)]
    private void FillPlacements()
    {
        _enemyPositions = new Transform[_enemyPositionsContainer.childCount];
        for (int i = 0; i < _enemyPositionsContainer.childCount; i++)
        {
            _enemyPositions[i] = _enemyPositionsContainer.GetChild(i);
            _enemyPositions[i].name = "EnemyPlacement_" + (i + 1);
        }
        MyDebug.LogGreen("[LevelController] Enemy positions filled");

        if (_playerPositionsContainer.childCount > 0)
        {
            _startPlayersPosition = _playerPositionsContainer.GetChild(0);
            _startPlayersPosition.name = "PlayersStart";
        }

        _playerPositions = new Transform[_playerPositionsContainer.childCount - 1];
        for (int i = 0; i < _playerPositionsContainer.childCount - 1; i++)
        {
            _playerPositions[i] = _playerPositionsContainer.GetChild(i+1);
            _playerPositions[i].name = "PlayerPlacement_" + (i + 1);
        }

        MyDebug.LogGreen("[LevelController] Player positions filled");
    }

    #endregion
}
