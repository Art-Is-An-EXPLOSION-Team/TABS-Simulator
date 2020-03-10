using UnityEngine;
using MLAgents;

public class PenguinAgent : Agent, Damageble
{
    [Header("AgentProperties")]

    public int health = 100;
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;

    [Header("AttackProperties")]

    public float normalAttackRange = 20f;
    public int normalAttackDamage = 40;
    public float normalAttackCD = 2f;

    private float normalAttackTimeCounter = 0f;
    private float detectionCounter = 0f;

    private TeamID teamID;
    public bool alive { get; set; }

    private WarriorArea warriorArea;
    private WarriorSettings m_warriorSettings;
    private BehaviorParameters m_BehaviorParameters;

    new private Rigidbody rigidbody;
    public Transform bulletSpawnPos;
    public GameObject bulletPrefab;

    public override void InitializeAgent()
    {
        base.InitializeAgent();
        warriorArea = GetComponentInParent<WarriorArea>();
        m_warriorSettings = FindObjectOfType<WarriorSettings>();
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        teamID = (TeamID)m_BehaviorParameters.m_TeamID;
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.maxAngularVelocity = 500;

        SetResetParams();
    }

    /// <summary>
    /// Perform actions based on a vector of numbers
    /// </summary>
    /// <param name="vectorAction">The list of actions to take</param>
    public override void AgentAction(float[] vectorAction)
    {
        if (alive == false) return;
        MoveAgent(vectorAction);
    }

    public void MoveAgent(float[] vectorAction)
    {
        float vertical = vectorAction[0] - 1;
        float horizontal = vectorAction[1] - 1;
        float turn = vectorAction[2] - 1;
        float attack = vectorAction[3];

        Vector3 dir = (vertical * transform.forward + horizontal * transform.right).normalized;

        // Apply movement
        // rigidbody.MovePosition(transform.position + dir * moveSpeed * Time.fixedDeltaTime);
        if (rigidbody.velocity.sqrMagnitude < moveSpeed * moveSpeed)
            rigidbody.AddForce(dir * 4f, ForceMode.VelocityChange);

        transform.Rotate(transform.up * turn * turnSpeed * Time.fixedDeltaTime);

        if (Time.time > detectionCounter + 0.5f)
        {
            detectionCounter = Time.time;
            if (MeleeRangeDetection())
            {
                AddReward(0.02f);
            }
            else
                AddReward(-0.02f);
        }

        if (attack == 1f && Time.time > normalAttackTimeCounter + normalAttackCD)
        {
            normalAttackTimeCounter = Time.time;
            if (MeleeRangeDetection())
            {
                MeleeAttack();
            }
            else
                AddReward(-0.02f);
        }

        // Apply a tiny negative reward every step to encourage action
        AddReward(-1f / 5000f);
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
    public override void AgentReset()
    {
        warriorArea.ResetAreaParams();
        SetResetParams();

        gameObject.tag = teamID == TeamID.TeamOne ? "TeamOne" : "TeamTwo";
        transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        transform.position = warriorArea.GetSpawnPos(teamID);
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    private bool MeleeRangeDetection()
    {
        float accuracy = 2;
        float angle = 30f;

        //一条向前的射线
        if (LineCast(Quaternion.identity, Color.green, normalAttackRange))
            return true;

        //多一个精确度就多两条对称的射线,每条射线夹角是总角度除与精度
        float subAngle = (angle / 2) / accuracy;
        for (int i = 0; i < accuracy; i++)
        {
            if (LineCast(Quaternion.Euler(0, -1 * subAngle * (i + 1), 0), Color.green, normalAttackRange)
                || LineCast(Quaternion.Euler(0, subAngle * (i + 1), 0), Color.green, normalAttackRange))
                return true;
        }
        return false;
    }

    public bool LineCast(Quaternion eulerAnger, Color DebugColor, float attackRange)
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, eulerAnger * transform.forward, out hit, attackRange) && !hit.collider.CompareTag(gameObject.tag))
        {
            if (hit.collider.CompareTag("TeamOne") || hit.collider.CompareTag("TeamTwo"))
                return true;

        }
        return false;
    }


    /// <summary>
    /// 智能代理受伤调用的函数。代理死亡时返回true，
    /// </summary>
    /// <param name="damage">遭受的伤害值</param>
    public bool Damaged(int damage)
    {
        health -= damage;
        //AddReward(-(float)(damage / health));

        if (health < 0)
        {
            warriorArea.UpdateStatus(gameObject.tag);
            gameObject.tag = "dead";
            alive = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 智能代理攻击函数。若对象已死亡则跳过逻辑
    /// </summary>    
    public void MeleeAttack()
    {

    }

    private void SetResetParams()
    {
        health = 100;
        alive = true;

        //cirrculum learning setting
        moveSpeed = Academy.Instance.FloatProperties.GetPropertyWithDefault("move_speed", moveSpeed);
        normalAttackRange = Academy.Instance.FloatProperties.GetPropertyWithDefault("attack_range", normalAttackRange);
    }

}