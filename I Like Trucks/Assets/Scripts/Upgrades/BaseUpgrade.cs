using System;
using UnityEngine;

public abstract class BaseUpgrade : ScriptableObject
{
    protected GameObject player;
    protected CarInfo stats;
    [SerializeField] protected UpgradeInfo[] info;
    private UpgradeInfo.ChangeType changeType;
    private float value;

    public void Start()
    {
        player = GameplayManager.Instance.player;
        stats = CarManager.Instance.info;
    }

    public abstract void OnCreate();
    public virtual void OnApply()
    {
        foreach (UpgradeInfo upgrade in info)
        {
            changeType = upgrade.type;
            value = upgrade.value;

            switch(upgrade.stat)
            {
                case UpgradeInfo.Stats.accelerationMultiplier:
                    AddValue(ref stats.accelerationMultiplier);
                    break;
                case UpgradeInfo.Stats.maxSpeed:
                    AddValue(ref stats.maxSpeed);
                    break;
                case UpgradeInfo.Stats.springStrength:
                    AddValue(ref stats.springStrength);
                    break;
                case UpgradeInfo.Stats.springLength:
                    AddValue(ref stats.springLength);
                    break;
                case UpgradeInfo.Stats.springDampening:
                    AddValue(ref stats.springDampening);
                    break;
                case UpgradeInfo.Stats.wheelRadius:
                    AddValue(ref stats.wheelRadius);
                    break;
                case UpgradeInfo.Stats.traction:
                    AddValue(ref stats.traction);
                    break;
                case UpgradeInfo.Stats.tireMass:
                    AddValue(ref stats.tireMass);
                    break;
                case UpgradeInfo.Stats.tireFriction:
                    AddValue(ref stats.tireFriction);
                    break;
                case UpgradeInfo.Stats.brakePower:
                    AddValue(ref stats.brakePower);
                    break;
                case UpgradeInfo.Stats.steeringPower:
                    AddValue(ref stats.steeringPower);
                    break;
                case UpgradeInfo.Stats.maxSteeringAngle:
                    AddValue(ref stats.maxSteeringAngle);
                    break;
                case UpgradeInfo.Stats.resistance:
                    AddValue(ref stats.resistance);
                    break;
            }

        }

        CarManager.SyncTires();

    }
    public abstract void OnRemove();

    public void AddScript(Type script)
    {
        player.AddComponent(script);
    }
    public void AddValue(ref float stat)
    {
        Debug.Log($"{name} editing {stat} {changeType} {value}");
        switch(changeType)
        {
            case UpgradeInfo.ChangeType.Add:
                stat += value;
                break;
            case UpgradeInfo.ChangeType.AddMulti:
                stat *= value;
                break;
            case UpgradeInfo.ChangeType.TrueMulti:
                break;
        }
    }
}
