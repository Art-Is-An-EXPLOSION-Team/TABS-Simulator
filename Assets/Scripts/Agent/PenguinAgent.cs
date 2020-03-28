using UnityEngine;
using MLAgents;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using MLAgents.Policies;

public class PenguinAgent : AgentECS
{
    [Header("AttackProperties")]
    public float normalAttackRange = 20f;
    public int normalAttackDamage = 20;
    public int spreadAmount = 10;
    public float normalAttackCD = 2f;

    private float normalAttackTimeCounter = 0f;
    private float detectionCounter = 0f;

    public Transform bulletSpawnPos;
    public GameObject bulletPrefab;
    private Entity bulletEntityPrefab;

    #region  ML-Agents

    public override void Initialize()
    {
        base.Initialize();

        var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        bulletEntityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(bulletPrefab, settings);

        SetResetParams();
    }

    /// <summary>
    /// Perform actions based on a vector of numbers
    /// </summary>
    /// <param name="vectorAction">The list of actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        if (Alive == false || agentEntity == new Entity()) return;
        MoveAgent(vectorAction);
    }

    public void MoveAgent(float[] vectorAction)
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
        rigidbody.MovePosition(transform.position + dir * moveSpeed * Time.deltaTime);
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


    /// <summary>
    /// Reset the agent and area
    /// </summary>
    public override void OnEpisodeBegin()
    {
        area.OnEpisodeBegin();
        SetResetParams();

        gameObject.tag = teamID == TeamID.TeamOne ? "TeamOne" : "TeamTwo";
        transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        transform.position = area.GetSpawnPos(teamID);
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    private void SetResetParams()
    {
        //Agent's reward is reset to 0 on each episode's beginning
        if (agentEntity != new Entity())
            entityManager.SetComponentData(agentEntity, new AgentData { Reward = 0, teamID = this.teamID, health = this.health });
        //entityManager.CompleteAllJobs();
        SetAnimation("Idle");

        //cirrculum learning settings
        moveSpeed = Academy.Instance.FloatProperties.GetPropertyWithDefault("move_speed", moveSpeed);
        normalAttackRange = Academy.Instance.FloatProperties.GetPropertyWithDefault("attack_range", normalAttackRange);
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