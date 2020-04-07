using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(MoveForwardSystem))]
public class TimedDestroySystem : SystemBase
{
    EndSimulationEntityCommandBufferSystem buffer;

    protected override void OnCreate()
    {
        buffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    public void DestroyProjectile(Entity entity)
    {
        Entities.ForEach((ref TimeToLive timeToLive, in ProjectileData projectileData) =>
                {
                    if (projectileData.parent == entity)
                        timeToLive.Value = 0f;
                }).ScheduleParallel();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.Concurrent commands = buffer.CreateCommandBuffer().ToConcurrent();
        float dt = Time.DeltaTime;
        Entities.ForEach((Entity entity, int entityInQueryIndex, ref TimeToLive timeToLive) =>
        {
            timeToLive.Value -= dt;
            if (timeToLive.Value <= 0f)
                commands.DestroyEntity(entityInQueryIndex, entity);
        }).ScheduleParallel();
        buffer.AddJobHandleForProducer(Dependency);
    }
}

