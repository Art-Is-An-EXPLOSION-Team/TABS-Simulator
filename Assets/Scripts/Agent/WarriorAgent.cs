using UnityEngine;
using MLAgents;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using MLAgents.Policies;

public class WarriorAgent : AgentECS
{

    [Header("AttackProperties")]
    public float meleeAttackRange = 10f;
    public int meleeAttackDamage = 40;
    public float meleeAttackCD = 2f;
    public float meleeAttackForce = 5f;

    private float meleeAttackTimeCounter = 0f;
    private float detectionCounter = 0f;


    /// <summary>
    /// Initial setup, called when the agent is enabled
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
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

        if (Time.time > detectionCounter + 0.5f)
        {
            detectionCounter = Time.time;
            if (RangeDetection(detectionAccuracy, detectionAngle, meleeAttackRange))
            {
                AddReward_Ecs(0.02f);
            }
            else
                AddReward_Ecs(-0.02f);
        }

        if (attack == 1f && Time.time > meleeAttackTimeCounter + meleeAttackCD)
        {
            meleeAttackTimeCounter = Time.time;
            if (RangeDetection(detectionAccuracy, detectionAngle, meleeAttackRange, true))
            {
                detectedTarget.GetComponent<Rigidbody>().AddForce(transform.forward * meleeAttackForce, ForceMode.Impulse);
                MeleeAttack();
            }
            else
                AddReward_Ecs(-0.02f);
        }
    }

    /// <summary>
    /// Read inputs from the keyboard and convert them to a list of actions.
    /// This is called only when the player wants to control the agent and has set
    /// Behavior Type to "Heuristic Only" in the Behavior Parameters inspector.
    /// </summary>
    /// <returns>A vectorAction array of floats that will be passed into <see cref="AgentAction(float[])"/></returns>
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

    /// <summary>
    /// Reset the agent and area
    /// </summary>
    public override void OnEpisodeBegin()
    {
        area.OnEpisodeBegin();
        SetResetParams();

        SetAnimation("Reset");
        gameObject.tag = teamID == TeamID.TeamOne ? "TeamOne" : "TeamTwo";
        transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        transform.position = area.GetSpawnPos(teamID);
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    /// <summary>
    /// 智能代理攻击函数。若对象已死亡则跳过逻辑
    /// </summary>    
    public void MeleeAttack()
    {
        SetAnimation("isAttacking");
        if (detectedTarget == null) return;
        var script = detectedTarget.GetComponent<AgentECS>();
        if (script.Alive == false) return;

        AddReward_Ecs(0.5f);
        script.Damaged(meleeAttackDamage);
        detectedTarget = null;
    }

    private void SetResetParams()
    {
        if (agentEntity != new Entity())
            entityManager.SetComponentData(agentEntity, new AgentData { Reward = 0, teamID = this.teamID, health = this.health });

        //cirrculum learning setting
        moveSpeed = Academy.Instance.FloatProperties.GetPropertyWithDefault("move_speed", moveSpeed);
        meleeAttackRange = Academy.Instance.FloatProperties.GetPropertyWithDefault("attack_range", meleeAttackRange);
    }

}