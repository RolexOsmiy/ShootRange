using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "GameData", menuName = "ScriptableObjects/GameData", order = 1)]
public class GameData : ScriptableObject
{
    public int startUnitCost = 50;
    public float costMultiplierPerOneBuy = 0.35f;
    public int buysCount = 1;

    [Header("Economy")] 
    public float coinsAwardMultiplier = 1.0738f;

    public void BuyUnit()
    {
        buysCount++;
        StatePersister.Instance.PlayerCoins -= StatePersister.Instance.UnitCurrentCost;
    }
}