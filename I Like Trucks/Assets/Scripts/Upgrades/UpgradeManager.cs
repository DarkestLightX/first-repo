using UnityEngine;
using System;

public class UpgradeManager : MonoBehaviour
{
    [Serializable]
    public class UpgradeDictionary : SerializableDictionary<int, Upgrade>{}
    public UpgradeDictionary upgrades = new();

    public void ChooseUpgrade(int index)
    {
        Upgrade upgrade = upgrades.Dictionary[index];

        upgrade.Start();
        upgrade.OnApply();
    }
}

