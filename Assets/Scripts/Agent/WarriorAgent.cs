using UnityEngine;
using MLAgents;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using MLAgents.Policies;
using MLAgents.SideChannels;

public class WarriorAgent : AgentECS
{

    [Header("AttackProperties")]
    public float meleeAttackRange = 10f;
    public int meleeAttackDamage = 40;
    public float meleeAttackCD = 2f;
    public float meleeAttackForce = 5f;

    protected float meleeAttackTimeCounter;
    private bool m_Attack = false;
    public float hitReward = 0.2f;

    FloatPropertiesChannel m_FloatProperties;

    /// <summary>
    /// Initial setup, called when the agent is enabled
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        m_FloatProperties = SideChannelUtils.GetSideChannel<FloatPropertiesChannel>();
        SetResetParams();
    }

    public override void CollectObservations(MLAgents.Sensors.VectorSensor sensor)
    {
        sensor.AddObservation(System.Convert.ToInt32(Alive));
        sensor.AddObservation(System.Convert.ToInt32(m_Attack));
    }

    protected override void PerformAction(float[] vectorAction)
    {
        //state properties
        m_Attack = false;

        //action properties
        float vertical = vectorAction[0] - 1;
        float horizontal = vectorAction[1] - 1;
        float turn = vectorAction[2] - 1;
        float attack = vectorAction[3];

        Vector3 dir = (vertical * transform.forward + horizontal * transform.right).normalized;
        if (dir != Vector3.zero)
            SetAnimation("isRunning");
        else
            SetAnimation("Idle");

        // if (Time.time > detectionCounter + detectionCD)
        // {
        //     detectionCounter = Time.time;
        //     if (RangeDetection(detectionAccuracy, detectionAngle, meleeAttackRange))
        //     {
        //         AddReward_Ecs(0.02f);
        //     }
        // }

        if (attack == 1f && Time.time > meleeAttackTimeCounter + meleeAttackCD)
        {
            meleeAttackTimeCounter = Time.time;
            m_Attack = true;

            var attackDir = meleeAttackRange * transform.forward;
            Debug.DrawRay(transform.position, attackDir, Color.red, 0.2f, true);

            if (SphereLineCast(6, 60f, meleeAttackRange))
            {
                MeleeAttack(detectedTarget);
                detectedTarget = null;
            }
            //else
            //AddReward_Ecs(-0.02f);//Add penalities when attack misses

            // if (RangeDetection(detectionAccuracy, detectionAngle, meleeAttackRange, true))
            // {
            //     detectedTarget.GetComponent<Rigidbody>().AddForce(transform.forward * meleeAttackForce, ForceMode.VelocityChange);              
            //     MeleeAttack();
            // }
            // else
            //     AddReward_Ecs(-0.02f);
        }

        // Apply movement
        agentRb.AddForce(dir * moveSpeed / 5f, ForceMode.VelocityChange);
        if (agentRb.velocity.sqrMagnitude > 16f) agentRb.velocity *= 0.95f;
        transform.Rotate(transform.up * turn * turnSpeed * Time.fixedDeltaTime);
        //agentRb.MovePosition(transform.position + dir * moveSpeed * Time.fixedDeltaTime);

        AddReward_Ecs(-1f / maxStep);
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
        {
            // move forward
            attack = 1f;
        }

        // Put the actions into an array and return
        return new float[] { vertical, horizontal, turn, attack };
    }

    public override void SetResetParams()
    {
        meleeAttackTimeCounter = Time.time;
        //cirrculum learning setting
        moveSpeed = m_FloatProperties.GetPropertyWithDefault("move_speed", moveSpeed);
        meleeAttackRange = m_FloatProperties.GetPropertyWithDefault("attack_range", meleeAttackRange);
    }

    /// <summary>
    /// 智能代理攻击函数。若对象已死亡则跳过逻辑
    /// </summary>
    public void MeleeAttack(Transform target)
    {
        SetAnimation("isAttacking");
        var script = target.GetComponent<AgentECS>();
        if (script == null || script.Alive == false) return;

        target.GetComponent<Rigidbody>().AddForce(transform.forward * meleeAttackForce, ForceMode.VelocityChange);
        if (IsOpponent(target.gameObject))
            AddReward_Ecs(hitReward);
        //else
        //AddReward_Ecs(-0.02f);

        script.Damaged(meleeAttackDamage);
    }
    public override void Damaged(int damage)
    {
        var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
        agentComponent.health -= damage;
        agentComponent.Reward += -hitReward;
        entityManager.SetComponentData(agentEntity, agentComponent);
    }

    public bool SphereLineCast(float accuracy, float angle, float attackRange)
    {
        //一条向前的射线
        if (LineCast(Quaternion.identity, Color.green, attackRange)) return true;

        //多一个精确度就多两条对称的射线,每条射线夹角是总角度除与精度
        float subAngle = (angle / 2) / accuracy;
        for (int i = 0; i < accuracy; i++)
        {
            if (LineCast(Quaternion.Euler(0, -1 * subAngle * (i + 1), 0), Color.green, attackRange)
                || LineCast(Quaternion.Euler(0, subAngle * (i + 1), 0), Color.green, attackRange))
                return true;
        }
        return false;
    }

    public bool LineCast(Quaternion eulerAnger, Color DebugColor, float attackRange)
    {
        UnityEngine.RaycastHit hit;
        Debug.DrawRay(transform.position, eulerAnger * transform.forward * attackRange, DebugColor, 0.3f);
        if (Physics.Raycast(transform.position, eulerAnger * transform.forward, out hit, attackRange))
        {
            if (hit.collider.CompareTag("TeamOne") || hit.collider.CompareTag("TeamTwo"))
            {
                detectedTarget = hit.collider.transform;
                return true;
            }
        }
        return false;
    }
}