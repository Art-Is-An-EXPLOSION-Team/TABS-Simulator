using System.Collections;
using Unity.Entities;

[GenerateAuthoringComponent]
public struct TimeToLiveComponent : IComponentData
{
    public float Value;
}
