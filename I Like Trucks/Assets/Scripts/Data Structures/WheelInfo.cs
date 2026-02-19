using System;
using UnityEngine;

[Serializable]
public struct WheelInfo
{
    [Header("Suspension")]
    public float wheelRadius;
    public float springLength;
    public float springStrength;
    public float springDampening;

    [Header("Traction")]
    public float traction;
    public float tireMass;
    public float camber;
    public float caster;
    public float toeIn;

    [Header("Acceleration and Steering")]

    public float tireFriction;
    public float brakePower;
    public float maxSpeed;
    public float accelerationMultiplier;
    public float powerCurve;
    public float maxSteeringAngle;
    public float steeringPower;
    public float returnSpeed;

    [Header("Other Fields")]
    public bool isRight;
    public bool canDrive;
    public bool canSteer;
}
