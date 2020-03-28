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

    // struct CullingJob : IJobForEachWithEntity<TimeToLive>
    // {
    //     public EntityCommandBuffer.Concurrent commands;
    //     public float dt;

    //     public void Execute(Entity entity, int jobIndex, ref TimeToLive timeToLive)
    //     {
    //         timeToLive.Value -= dt;
    //         if (timeToLive.Value <= 0f)
    //             commands.DestroyEntity(jobIndex, entity);
    //     }
    // }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.Concurrent commands = buffer.CreateCommandBuffer().ToConcurrent();
        float dt = Time.DeltaTime;
        Entities.ForEach((Entity entity, int entityInQueryIndex, ref TimeToLive timeToLive) =>
        {
            timeToLive.Value -= dt;
            if (timeToLive.Value <= 0f)
                commands.DestroyEntity(0, entity);
        }).ScheduleParallel();
        buffer.AddJobHandleForProducer(Dependency);
    }
}

