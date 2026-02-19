using System;
using UnityEngine;

[Serializable]
public class CarInfo
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
    public float maxSteeringAngle;
    public float steeringPower;
    public float returnSpeed;
    [Header("Damage")]
    public float resistance;
}
