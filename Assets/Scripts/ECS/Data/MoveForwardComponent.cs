using System;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct MoveForward : IComponentData
{
    public float Speed;
}

