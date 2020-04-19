using UnityEngine;
using MLAgents;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using MLAgents.Policies;
using MLAgents.SideChannels;

public class PenguinAgent : AgentECS
{
    [Header("AttackProperties")]
    public float normalAttackRange = 20f;
    public int normalAttackDamage = 20;
    public int spreadAmount = 10;
    public float normalAttackCD = 2f;

    private float normalAttackTimeCounter = 0f;

    public Transform bulletSpawnPos;
    public GameObject bulletPrefab;
    private Entity bulletEntityPrefab;

    TimedDestroySystem timedDestroySystem;
    FloatPropertiesChannel m_FloatProperties;

    #region  ML-Agents

    public override void Initialize()
    {
        base.Initialize();

        var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        bulletEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(bulletPrefab, settings);
        timedDestroySystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<TimedDestroySystem>();
        //m_FloatProperties = SideChannelUtils.GetSideChannel<FloatPropertiesChannel>();

        SetResetParams();
    }

    protected override void PerformAction(float[] vectorAction)
    {
        float vertical = vectorAction[0] - 1;
        float horizontal = vectorAction[1] - 1;
        float turn = vectorAction[2] - 1;
        float attack = vectorAction[3];

        Vector3 dir = (vertical * transform.forward + horizontal * transform.right).normalized;
        if (dir != Vector3.zero)
            SetAnimation("isRunning");
        else
            SetAnimation("Idle");

        // Apply movement
        agentRb.MovePosition(transform.position + dir * moveSpeed * Time.deltaTime);
        transform.Rotate(transform.up * turn * turnSpeed * Time.deltaTime);

        if (Time.time > detectionCounter + detectionCD)
        {
            detectionCounter = Time.time;
            if (RangeDetection(detectionAccuracy, detectionAngle, normalAttackRange))
            {
                var bonus = 0.02f;
                AddReward_Ecs(bonus);
            }
            else
            {
                var bonus = -0.02f;
                AddReward_Ecs(bonus);
            }
        }

        if (attack == 1f && Time.time > normalAttackTimeCounter + normalAttackCD)
        {
            normalAttackTimeCounter = Time.time;
            //Vector3 bulletEuler = bulletSpawnPos.rotation.eulerAngles;
            //bulletEuler.x = 0f;
            //NormalAttack();
            SetAnimation("isShaking");
            SpreadAttack();
        }
    }

    public override float[] Heuristic()
    {
        float vertical = 1f;
        float horizontal = 1f;
        float turn = 1f;

        if (Input.GetKey(KeyCode.S))
        {
            // move forward
            vertical = 0f;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            // move backward
            vertical = 2f;
        }

        // if (Input.GetKey(KeyCode.A))
        // {
        //     // move left
        //     horizontal = 0f;
        // }
        // else if (Input.GetKey(KeyCode.D))
        // {
        //     // move right
        //     horizontal = 2f;
        // }

        if (Input.GetKey(KeyCode.Q))
        {
            // turn left
            turn = 0f;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            // turn right
            turn = 2f;
        }

        float attack = 0;
        if (Input.GetKey(KeyCode.Space))
            attack = 1f;

        // Put the actions into an array and return
        return new float[] { vertical, horizontal, turn, attack };
    }

    public override void SetResetParams()
    {
        SetAnimation("Idle");
        if (agentEntity != new Entity())
        {
            //Clear the existing useless Projectile
            timedDestroySystem.DestroyProjectile(agentEntity);
        }
        //cirrculum learning settings      
        //moveSpeed = m_FloatProperties.GetPropertyWithDefault("move_speed", moveSpeed);
        //normalAttackRange = m_FloatProperties.GetPropertyWithDefault("attack_range", normalAttackRange);
    }

    #endregion

    #region Attack

    /// <summary>
    /// 智能代理攻击函数。若对象已死亡则跳过逻辑
    /// </summary>    
    // public void NormalAttack()
    // {
    //     Entity bullet = entityManager.Instantiate(bulletEntityPrefab);

    //     entityManager.SetComponentData(bullet, new Translation { Value = bulletSpawnPos.position });
    //     entityManager.SetComponentData(bullet, new Rotation { Value = Quaternion.Euler(bulletSpawnPos.rotation.eulerAngles) });
    // }

    public void SpreadAttack()
    {
        int max = spreadAmount / 2;
        int min = -max;
        int totalAmount = 2 * spreadAmount;
        int index = 0;

        var bulletEuler = bulletSpawnPos.rotation.eulerAngles;
        bulletEuler.x = 0f;
        Vector3 tempEuler = bulletEuler;

        NativeArray<Entity> bullets = new NativeArray<Entity>(totalAmount, Allocator.TempJob);
        entityManager.Instantiate(bulletEntityPrefab, bullets);

        var trans = new Translation { Value = bulletSpawnPos.position };
        for (int y = min; y < max; y++)
        {
            tempEuler.y = (bulletEuler.y + 3 * y) % 360;

            // var bullets[index] = entityManager.Instantiate(bulletEntityPrefab);
            entityManager.SetComponentData(bullets[index], trans);
            entityManager.SetComponentData(bullets[index], new Rotation { Value = Quaternion.Euler(tempEuler) });
            entityManager.AddComponentData(bullets[index], new MoveForward { Speed = normalAttackRange });

            var data = entityManager.GetComponentData<ProjectileData>(bullets[index]);
            data.parent = agentEntity;
            data.damage = normalAttackDamage;
            entityManager.SetComponentData(bullets[index], data);

            index++;
        }

        bullets.Dispose();
    }

    #endregion
}