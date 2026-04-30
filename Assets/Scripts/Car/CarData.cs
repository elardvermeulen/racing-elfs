using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


public struct CarData : IComponentData
{
    public float acceleration;
    public float turnSpeed;
    public float3 startModelOffset;
    public float lateralDampning;

    public float groundCheckRate;
    public float lastGroundCheckTime;

    public float curYRot;
}
