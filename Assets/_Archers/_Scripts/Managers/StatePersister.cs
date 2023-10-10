using System;
using System.Linq;
using SecPlayerPrefs;

public class StatePersister : HMSingleton<StatePersister>
{
    public bool TapticEnabled = true;

    private const string kCurrentLevel = "CurrentLevel";
    private const string kUnitsSave = "UnitsSave";
    private const string kPlayerCoins = "PlayerCoins";
    private const string kUnitCost = "UnitCost";
    private const string kAwardForLevelFinish = "AwardForLevelFinish";

    public int CurrentLevel
    {
        get => SecurePlayerPrefs.HasKey(kCurrentLevel) ? SecurePlayerPrefs.GetInt(kCurrentLevel) : 1;
        set => SecurePlayerPrefs.SetInt(kCurrentLevel, value);
    }
    
    public int PlayerCoins
    {
        get => SecurePlayerPrefs.HasKey(kPlayerCoins) ? SecurePlayerPrefs.GetInt(kPlayerCoins) : 1;
        set => SecurePlayerPrefs.SetInt(kPlayerCoins, value);
    } 
    
    public int AwardForLevelFinish
    {
        get => SecurePlayerPrefs.HasKey(kAwardForLevelFinish)
            ? SecurePlayerPrefs.GetInt(kAwardForLevelFinish)
            : MainGameManager.Instance.gameData.startUnitCost;
        set => SecurePlayerPrefs.SetInt(kAwardForLevelFinish, value);
    }
    
    public int UnitCurrentCost
    {
        //60 x (1 + 0.35)^3 = 144.99
        get => SecurePlayerPrefs.HasKey(kUnitCost)
            ? SecurePlayerPrefs.GetInt(kUnitCost)
            : GetUnitCurrentCost();
        set => SecurePlayerPrefs.SetInt(kUnitCost, value);
    } 

    public int[] unitsSave = new int[36];
    
    // Start is called before the first frame update
    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        LoadUnitPosition();
    }

    public int GetUnitCurrentCost()
    {
        var cost = Convert.ToInt32(MainGameManager.Instance.gameData.startUnitCost *
                                   Math.Pow((1 + MainGameManager.Instance.gameData.costMultiplierPerOneBuy),
                                       MainGameManager.Instance.gameData.buysCount));
        UnitCurrentCost = cost;
        
        return cost;
    }

    #region UnitSaveLoad

    //Set array of unit positions
    public void SaveUnitPosition(int unitIndex, int unitLevel)
    {
        unitsSave[unitIndex] = unitLevel;
        
        string[] resultArray = unitsSave.Select(i => i.ToString()).ToArray();
        string resultString = String.Join(", ", resultArray);
        SecurePlayerPrefs.SetString(kUnitsSave, resultString);
        
        MyDebug.LogGreen("Units saved: " + resultString);
    }

    //Get array of unit positions
    public int[] LoadUnitPosition()
    {
        string str = "";

        if (SecurePlayerPrefs.HasKey(kUnitsSave)) // Проверяем есть ли сохранение
        {
            str = SecurePlayerPrefs.GetString(kUnitsSave);
        }
        else //Если нет еще сохранения то создаем массив с 1 юнитом 0 уровня.
        {
            for (int i = 0; i < unitsSave.Length; i++)
            {
                SaveUnitPosition(i,-1);
            }

            SaveUnitPosition(0,0);
            
            str = SecurePlayerPrefs.GetString(kUnitsSave);
        }
        unitsSave = Array.ConvertAll(str.Split(','), int.Parse);

        return unitsSave;
    }

    #endregion
}
