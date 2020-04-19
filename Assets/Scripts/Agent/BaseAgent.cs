using UnityEngine;
using MLAgents;
using MLAgents.Policies;

public class BaseAgent : Agent
{
    [Header("AgentProperties")]
    public int maxHealth = 100;
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;
    public TeamID teamID;
    public bool Alive = true;

    public int currentHealth;

    [Header("RangeProperties")]
    public float detectionAccuracy = 6f;
    public float detectionAngle = 60f;
    public float detectionCD = 0.5f;
    protected float detectionCounter = 0f;

    protected Rigidbody agentRb;
    protected Transform detectedTarget;

    protected Area area;

    protected BehaviorParameters m_BehaviorParameters;
    protected Animator m_Aniamtor;

    public override void Initialize()
    {
        base.Initialize();

        area = GetComponentInParent<Area>();
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        teamID = (TeamID)m_BehaviorParameters.TeamId;
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;
        m_Aniamtor = GetComponent<Animator>();
    }

    /// <summary>
    /// Perform actions based on a vector of numbers
    /// </summary>
    /// <param name="vectorAction">The list of actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        if (currentHealth <= 0 && Alive)
        {
            gameObject.tag = "dead";
            Alive = false;
            SetAnimation("isDead");
            area.UpdateStatus(teamID);           

            gameObject.layer = LayerMask.NameToLayer("Dead");
        }

        if (Alive)
        {   //PerformAction
            PerformAction(vectorAction);
        }

        AddReward(-1f / maxStep);
    }

    /// <summary>
    /// Reset the agent and area
    /// </summary>
    public override void OnEpisodeBegin()
    {
        area.OnEpisodeBegin();
        SetResetParams();

        gameObject.tag = teamID == TeamID.TeamOne ? "TeamOne" : "TeamTwo";
        currentHealth = maxHealth;
        Alive = true;
        gameObject.layer = LayerMask.NameToLayer("Default");

        transform.rotation = teamID == TeamID.TeamTwo ? Quaternion.Euler(0f, UnityEngine.Random.Range(-60f, 60f), 0f)
            : Quaternion.Euler(0f, UnityEngine.Random.Range(120f, 240f), 0f);
        transform.position = area.GetSpawnPos(teamID);
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
    }

    public virtual void SetResetParams() { }

    protected virtual void PerformAction(float[] vectorAction) { }

    #region Utils

    public bool RangeDetection(float accuracy, float angle, float attackRange, bool meleeDetect = false)
    {
        //一条向前的射线
        if (LineCast(Quaternion.identity, Color.green, attackRange, meleeDetect))
            return true;

        //多一个精确度就多两条对称的射线,每条射线夹角是总角度除与精度
        float subAngle = (angle / 2) / accuracy;
        for (int i = 0; i < accuracy; i++)
        {
            if (LineCast(Quaternion.Euler(0, -1 * subAngle * (i + 1), 0), Color.green, attackRange, meleeDetect)
                || LineCast(Quaternion.Euler(0, subAngle * (i + 1), 0), Color.green, attackRange, meleeDetect))
                return true;
        }
        return false;
    }

    public bool LineCast(Quaternion eulerAnger, Color DebugColor, float attackRange, bool meleeDetect = false)
    {
        UnityEngine.RaycastHit hit;
        Debug.DrawRay(transform.position, eulerAnger * transform.forward * attackRange, DebugColor, 0.3f);

        if (Physics.Raycast(transform.position, eulerAnger * transform.forward, out hit, attackRange) && !hit.collider.CompareTag(gameObject.tag))
        {
            if (hit.collider.CompareTag("TeamOne") || hit.collider.CompareTag("TeamTwo"))
            {
                if (meleeDetect) detectedTarget = hit.transform;
                return true;
            }
        }
        return false;
    }

    public bool IsOpponent(GameObject target)
    {
        if (target.CompareTag("TeamOne") || target.CompareTag("TeamTwo"))
            if (!target.CompareTag(gameObject.tag)) return true;
        return false;
    }

    #endregion

    #region Animation

    private string currentAnimation = "";
    public void SetAnimation(string animationName)
    {
        if (currentAnimation != "")
        {
            m_Aniamtor.SetBool(currentAnimation, false);
        }

        if (animationName == "Reset")
        {
            m_Aniamtor.Play("Idle");
            m_Aniamtor.Update(0);
            return;
        }

        if (animationName != "Idle")
        {
            m_Aniamtor.SetBool(animationName, true);
            currentAnimation = animationName;
        }
        else
            currentAnimation = "";
    }

    #endregion

    #region Interface

    /// <summary>
    /// 智能代理受伤调用的函数。代理死亡时返回true (GameObejct交互接口)
    /// </summary>
    /// <param name="damage">遭受的伤害值</param>
    public virtual void Damaged(int damage)
    {
        //currentHealth -= damage;
        //AddReward(-hitan)      
    }

    #endregion

}
