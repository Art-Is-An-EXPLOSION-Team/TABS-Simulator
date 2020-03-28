using Unity.Entities;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct ProjectileData : IComponentData
{
    public Entity hitEffect;
    public Entity parent;
    public float timeToLive_Effect;
    public int damage;

}