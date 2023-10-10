using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class UnitsData : HMScriptableSingleton<UnitsData>
{
    public int maxUnitLevel; // from 0
    public int maxBuildingLevel; // from 0
    public int[] levelToHealth;
    public int[] levelBuildingToHealth;
    public int[] levelToArrows;
}
