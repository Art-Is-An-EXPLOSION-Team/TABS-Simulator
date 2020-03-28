using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Transforms
{
    public class MoveForwardSystem : JobComponentSystem
    {
        [BurstCompile]
        struct MoveForwardRotation : IJobForEach<Translation, Rotation, MoveForward>
        {
            public float dt;

            public void Execute(ref Translation pos, [ReadOnly] ref Rotation rot, [ReadOnly] ref MoveForward moveForward)
            {
                pos.Value = pos.Value + (dt * moveForward.Speed * math.forward(rot.Value));
            }
        }

        protected override JobHandle OnUpdate(JobHandle input)
        {
            float dt = Time.DeltaTime;

            MoveForwardRotation sd = new MoveForwardRotation
            {
                dt = Time.DeltaTime
            };

            return sd.Schedule(this, input);
            // Entities.ForEach((ref Translation pos, in Rotation rot, in MoveForward moveForward) =>
            // {
            //     pos.Value = pos.Value + (dt * moveForward.Speed * math.forward(rot.Value));
            // }).ScheduleParallel();

        }
    }
}