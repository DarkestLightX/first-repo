using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class AIWheelCaster : MonoBehaviour
{
    public List<WheelInfo> info = new();
    public List<Transform> transforms = new();
    public List<Vector3> tireVelocitys = new();
    public List<Vector3> speedInput = new();
    public List<Vector3> forceBuffer = new();
    public List<RaycastHit> hits = new();
    public List<int> toRemove;
    public static AIWheelCaster Instance;
    public static event Action onRemove;

    NativeArray<RaycastCommand> commands;
    NativeArray<RaycastHit> nativeHits;
    NativeArray<WheelInfo> nativeInfos;
    NativeArray<Quaternion> nativeTransforms;
    NativeArray<float3> nativeTireVelocitys;
    NativeArray<float3> nativeSpeedInputs;
    NativeArray<float3> nativeForceBuffer;

    private int count;

    //internal Arrays

    void Awake()
    {
        count = info.Count;

        commands = new NativeArray<RaycastCommand>(count, Allocator.Persistent);
        nativeHits = new NativeArray<RaycastHit>(count, Allocator.Persistent);
        nativeInfos = new NativeArray<WheelInfo>(count, Allocator.Persistent);
        nativeTransforms = new NativeArray<Quaternion>(count, Allocator.Persistent);
        nativeTireVelocitys = new NativeArray<float3>(count, Allocator.Persistent);
        nativeSpeedInputs = new NativeArray<float3>(count, Allocator.Persistent);
        nativeForceBuffer = new NativeArray<float3>(count, Allocator.Persistent);

        Instance = this;
    }

    //returns a kvp with the wheel id and rayvast information, intakes basic info
    public void FixedUpdate()
    {
        if (toRemove.Count > 0)
        {
            onRemove?.Invoke();
            toRemove.Sort();

            for (int i = toRemove.Count - 1; i > -1; i--)
            {
                int index = toRemove[i];

                info.RemoveAt(index);
                transforms.RemoveAt(index);
                tireVelocitys.RemoveAt(index);
                speedInput.RemoveAt(index);
            }

            toRemove.Clear();
        }

        int thisCount = info.Count;

        if (thisCount != count)
        {
            count = thisCount;

            commands.Dispose();
            nativeHits.Dispose();
            nativeInfos.Dispose();
            nativeTireVelocitys.Dispose();
            nativeTransforms.Dispose();
            nativeSpeedInputs.Dispose();
            nativeForceBuffer.Dispose();

            commands = new NativeArray<RaycastCommand>(count, Allocator.Persistent);
            nativeHits = new NativeArray<RaycastHit>(count, Allocator.Persistent);
            nativeInfos = new NativeArray<WheelInfo>(count, Allocator.Persistent);
            nativeTransforms = new NativeArray<Quaternion>(count, Allocator.Persistent);
            nativeTireVelocitys = new NativeArray<float3>(count, Allocator.Persistent);
            nativeSpeedInputs = new NativeArray<float3>(count, Allocator.Persistent);
            nativeForceBuffer = new NativeArray<float3>(count, Allocator.Persistent);
        }

        //internal counter

        for (int i = 0; i < info.Count; i++)
        {
            var infoValue = info[i];
            var transformValue = transforms[i];

            Vector3 origin = transformValue.position;
            Vector3 direction = -transformValue.up;
            float length = infoValue.wheelRadius;
            
            //Debug.Log($"Quaternion up: {transformValue.rotation * Vector3.up}, Transform up: {transformValue.up}");

            //holy moly magic number 7

            //Debug.Log($"Paired {key} with {i}");

            commands[i] = new RaycastCommand(origin, direction, new QueryParameters(7), length);
        }

        JobHandle rayHandle = RaycastCommand.ScheduleBatch(commands, nativeHits, 1, default);

        for (int i = 0; i < info.Count; i++)
        {
            nativeInfos[i] = info[i];
            nativeTransforms[i] = transforms[i].rotation;
            nativeTireVelocitys[i] = tireVelocitys[i];
            nativeSpeedInputs[i] = speedInput[i];
        }

        var wheelJob = new WheelJob
        {
            infos = nativeInfos,
            transforms = nativeTransforms,
            hits = nativeHits,
            tireVelocitys = nativeTireVelocitys,
            speedInput = nativeSpeedInputs,
            forceBuffer = nativeForceBuffer,
        };

        JobHandle wheelHandle = wheelJob.Schedule(count, 32, rayHandle);

        hits.Clear();
        forceBuffer.Clear();

        wheelHandle.Complete();

        for (int i = 0; i < info.Count; i++)
        {
            forceBuffer.Add(nativeForceBuffer[i]);
            hits.Add(nativeHits[i]);
            //Debug.Log($"Matched {kvp.Key} with {i}");
        }

    }

    void OnDestroy()
    {
        commands.Dispose();
        nativeHits.Dispose();
        nativeInfos.Dispose();
        nativeTireVelocitys.Dispose();
        nativeTransforms.Dispose();
        nativeSpeedInputs.Dispose();
        nativeForceBuffer.Dispose();
    }

    [BurstCompile]
    public struct WheelJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<WheelInfo> infos;
        [ReadOnly] public NativeArray<Quaternion> transforms;
        [ReadOnly] public NativeArray<RaycastHit> hits;
        [ReadOnly] public NativeArray<float3> tireVelocitys;
        [ReadOnly] public NativeArray<float3> speedInput; //(carSpeed, forwardIput, sideInput)
        [WriteOnly] public NativeArray<float3> forceBuffer;

        public void Execute(int i)
        {
            float upforce;
            float sideforce;
            float forwardforce = 0;
            bool grounded;

            grounded = hits[i].distance > 0f;
                
            float3 up = transforms[i] * new float3(0, 1, 0 ); //transform.up
            float3 forward = transforms[i] * new float3(0, 0, 1 ); //transform.forward
            float3 right = transforms[i] * new float3(1, 0 ,0);   //transform.right
            float3 tireVelocity = tireVelocitys[i];

            float forwardInput = speedInput[i].y;
            float carSpeed = speedInput[i].x;

            if (grounded)
            {
                float offset = infos[i].springLength - hits[i].distance;
                float upVel = math.dot(up, tireVelocity);
                upforce = (offset * infos[i].springStrength) - (upVel * infos[i].springDampening);

                if (upforce < 0)
                {
                    upforce = 0;
                } 

                float sideVel = math.dot(right, tireVelocity);
                float forwardVel = math.dot(forward, tireVelocity);
                //float desiredVel =  -sideVel * infos[i].traction.Evaluate(math.abs(math.clamp(forwardVel, 0f, 1f)));
                float desiredVel =  -sideVel * infos[i].traction;
                float desriedAccel = desiredVel;

                sideforce = desriedAccel * infos[i].tireMass;
            
                float normalizedSpeed = math.clamp(math.abs(carSpeed) / infos[i].maxSpeed, 0f, 1f);

                if (forwardInput > 0f && infos[i].canDrive && normalizedSpeed < 1f)
                {
                    //forwardforce = infos[i].powerCurve.Evaluate(normalizedSpeed) * forwardInput; -evaluate needs to be fixed
                    forwardforce = forwardInput * infos[i].accelerationMultiplier;
                }
                else if (forwardInput < 0f && infos[i].canDrive)
                {       
                    forwardforce = forwardInput * infos[i].brakePower;
                }
                else
                {
                    if (carSpeed > 0) 
                        forwardforce = -1 * infos[i].tireFriction;
                    if (carSpeed < 0)
                        forwardforce = 1 * infos[i].tireFriction;
                }
                
            }
            else
            {
                upforce = 0;
                sideforce = 0;
                forwardforce = 0;
            }

            forceBuffer[i] = new float3(sideforce, upforce, forwardforce);
        }
    }
}


