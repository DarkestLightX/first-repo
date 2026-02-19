using System;
using UnityEngine;

[Serializable]
public struct UpgradeInfo
{
    public float value;
    public Stats stat;
    public ChangeType type;

    [HideInInspector]
    public enum Stats
    {
        maxSpeed,
        accelerationMultiplier,
        springStrength,
        springLength,
        springDampening,
        wheelRadius,
        traction,
        tireMass,
        tireFriction,
        brakePower,
        steeringPower,
        maxSteeringAngle,
        resistance


    }
    [HideInInspector]
    public enum ChangeType
    {
        Add,
        AddMulti,
        TrueMulti
    }
}
