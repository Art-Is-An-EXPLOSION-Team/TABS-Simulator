using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using System;
[GenerateAuthoringComponent]
public struct AgentData : IComponentData
{
    public int health;
    public TeamID teamID;
    public float Reward;
}
