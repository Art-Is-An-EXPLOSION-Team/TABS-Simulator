using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Unity.Transforms;
using Unity.Profiling;
using Unity.Burst;
using System;
using UnityEditor;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class ProjectileCollisionSystem : JobComponentSystem
{
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery effectGroup;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        effectGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ProjectileData), }
        });
    }

    [BurstCompile]
    struct ProjectileCollisionJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentDataFromEntity<ProjectileData> ProjectileGroup;
        public ComponentDataFromEntity<AgentData> AgentGroup;
        public ComponentDataFromEntity<Translation> TranslationGroup;
        public EntityCommandBuffer.Concurrent commandBuffer;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.Entities.EntityA;
            Entity entityB = collisionEvent.Entities.EntityB;
            CheckProjectileToAgent(entityA, entityB);
            CheckProjectileToAgent(entityB, entityA);
        }

        void CheckProjectileToAgent(Entity entityA, Entity entityB)
        {
            bool isEntityA_Projectile = ProjectileGroup.Exists(entityA);
            if (isEntityA_Projectile)
            {
                var projectileData = ProjectileGroup[entityA];

                //Set Hit Effect
                var go = commandBuffer.Instantiate(0, projectileData.hitEffect);
                commandBuffer.SetComponent(0, go, new Translation { Value = TranslationGroup[entityA].Value });
                commandBuffer.AddComponent(0, go, new TimeToLive { Value = projectileData.timeToLive_Effect });
                commandBuffer.DestroyEntity(0, entityA);

                //Check if B is an Agent
                if (AgentGroup.Exists(entityB) && AgentGroup[entityB].health > 0)
                {
                    var agentAData = AgentGroup[projectileData.parent];
                    var agentBData = AgentGroup[entityB];

                    //Check their teams
                    if (agentAData.teamID == agentBData.teamID)
                        //temporarily reward 
                        agentAData.Reward -= projectileData.damage / 100f;
                    else
                        agentAData.Reward += 0.5f;

                    //Currently enabling Attcking teamates
                    agentBData.Reward -= projectileData.damage / 100f;
                    agentBData.health -= projectileData.damage;

                    //Set Agent Status 
                    if (agentBData.health <= 0) agentBData.teamID = TeamID.Dead;

                    AgentGroup[projectileData.parent] = agentAData;
                    AgentGroup[entityB] = agentBData;
                }
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle jobHandle = new ProjectileCollisionJob
        {
            ProjectileGroup = GetComponentDataFromEntity<ProjectileData>(true),
            AgentGroup = GetComponentDataFromEntity<AgentData>(),
            TranslationGroup = GetComponentDataFromEntity<Translation>(),
            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        }.Schedule(m_StepPhysicsWorldSystem.Simulation,
              ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        return jobHandle;
    }
}