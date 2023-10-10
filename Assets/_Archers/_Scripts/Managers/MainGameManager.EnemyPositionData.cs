using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class MainGameManager
{
    private class EnemyPositionData
    { 
        public int EnemiesLeft { get; private set; }
        private readonly int _placementIndex;
        private readonly Action<int> OnEnemyPlacementCleared;
        private readonly EnemyArcherController[] _enemyArchers;

        public EnemyPositionData(int[] archers, GameObject prefab, PositionsPlacementController placementController, int placementIndex, Action<int> onEnemyPlacementCleared)
        {
            /*int GetFreePlacementIndex(EnemyArcherController[] enemyArchers1)
            {
                int freeIndex = 0;
                while (enemyArchers1[freeIndex] != null)
                    freeIndex = Random.Range(0, kMaxEnemyArchersNumber);
                return freeIndex;
            }*/

            int CalcEnemiesLeft(IReadOnlyList<int> archers)
            {
                var enemies = 0;
                for (int i = 0; i < archers.Count; i++)
                    if (archers[i] >= 0)
                        enemies++;
                return enemies;
            }
            
            _enemyArchers = new EnemyArcherController[archers.Length];
            _placementIndex = placementIndex;
            EnemiesLeft = CalcEnemiesLeft(archers);
            OnEnemyPlacementCleared = onEnemyPlacementCleared;
        
            for (int i = 0; i < EnemiesLeft; i++)
            {
                if(archers[i] < 0 || archers[i] > UnitsData.Instance.maxUnitLevel)
                    continue;
                
                var enemy = Instantiate(prefab, placementController.transform);
                enemy.transform.position = placementController.GetPositionFor(i);
                _enemyArchers[i] = enemy.GetComponent<EnemyArcherController>();
                _enemyArchers[i].SetUnitLevel(archers[i]);
                _enemyArchers[i].onDeath = OnEnemyDead;
            }
        }

        private void OnEnemyDead(UnitController ctrl)
        {
            EnemiesLeft--;
            if (EnemiesLeft <= 0)
                OnEnemyPlacementCleared?.Invoke(_placementIndex);
        }

        public int GetArrowsNumber()
        {
            int arrowsNumber = 0;
            for (int i = 0; i < _enemyArchers.Length; i++)
            {
                var archer = _enemyArchers[i];

                if (archer != null)
                    arrowsNumber += archer.GetArrowsNumber();
            }

            return arrowsNumber;
        }
        
        public void RotateEnemiesTo(Vector3 targetPoint)
        {
            for (int j = 0; j < _enemyArchers.Length; j++)
            {
                var archer = _enemyArchers[j];

                if (archer != null)
                {
                    Transform transformTemp;
                    (transformTemp = archer.transform).LookAt(targetPoint, Vector3.up);
                    transformTemp.localRotation = Quaternion.Euler(0, transformTemp.localRotation.eulerAngles.y, 0);
                }    
            }
        }

        public List<Transform> GetAliveEnemiesTransforms()
        {
            var list = new List<Transform>();
            for (int i = 0; i < _enemyArchers.Length; i++)
            {
                if(_enemyArchers[i] != null && !_enemyArchers[i].IsDead)
                    list.Add(_enemyArchers[i].transform);
            }

            return list;
        }

        public void PrepareForShoot()
        {
            for (int i = 0; i < _enemyArchers.Length; i++)
            {
                if (_enemyArchers[i] != null && !_enemyArchers[i].IsDead)
                {
                    _enemyArchers[i].AnimatePrepareForShooting();
                    _enemyArchers[i].SwitchToMiddleTilt();
                }
            }
        }

        public void AnimateShooting()
        {
            for (int i = 0; i < _enemyArchers.Length; i++)
            {
                if (_enemyArchers[i] != null && !_enemyArchers[i].IsDead)
                {
                    _enemyArchers[i].AnimateShooting();
                    _enemyArchers[i].SwitchToMiddleTilt();
                }
            }
        }

        public void ClearArchers()
        {
            for (int i = 0; i < _enemyArchers.Length; i++)
            {
                if(_enemyArchers[i] != null)
                    Destroy(_enemyArchers[i].gameObject);
            }
        }

        public void KillEnemies()
        {
            for (int i = 0; i < _enemyArchers.Length; i++)
            {
                if (_enemyArchers[i] != null)
                    _enemyArchers[i].ApplyDamage(9999999); // deal maximum damage to all enemies to kill them
            }
        }
    }
}