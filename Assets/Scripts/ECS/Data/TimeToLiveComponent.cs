using System;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TimeToLive : IComponentData
{
    public float Value;
}
