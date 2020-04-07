using UnityEngine;
using MLAgents;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Physics;
using MLAgents.Policies;

public enum TeamID
{
    TeamOne = 0,
    TeamTwo = 1,
    Dead = 1 << 2
}

public class AgentECS : Agent, IReceiveEntity
{
    [Header("AgentProperties")]
    public int health = 100;
    public float moveSpeed = 5f;
    public float turnSpeed = 90f;
    public TeamID teamID;
    public bool Alive = true;

    [Header("RangeProperties")]
    public float detectionAccuracy = 5f;
    public float detectionAngle = 60f;
    public float detectionCD = 0.5f;
    protected float detectionCounter = 0f;

    protected Rigidbody agentRb;
    protected Transform detectedTarget;

    protected Area area;

    protected BehaviorParameters m_BehaviorParameters;
    protected Animator m_Aniamtor;

    protected EntityManager entityManager;
    protected BlobAssetStore blobAssetStore;
    public Entity agentEntity;

    public override void Initialize()
    {
        base.Initialize();

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        area = GetComponentInParent<Area>();
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        teamID = (TeamID)m_BehaviorParameters.TeamId;
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 500;
        m_Aniamtor = GetComponent<Animator>();

        blobAssetStore = new BlobAssetStore();
    }

    /// <summary>
    /// Perform actions based on a vector of numbers
    /// </summary>
    /// <param name="vectorAction">The list of actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        if (agentEntity != new Entity())
        {
            var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
            if (agentComponent.health <= 0 && Alive)
            {
                area.UpdateStatus(gameObject.tag);
                gameObject.tag = "dead";
                Alive = false;
                SetAnimation("isDead");
            }
            else if (agentComponent.health > 0)
            {
                gameObject.tag = teamID == TeamID.TeamOne ? "TeamOne" : "TeamTwo";
                Alive = true;
            }

            if (Alive)
            {   //PerformAction
                PerformAction(vectorAction);
            }
            //Update Agent's ECSBody
            entityManager.SetComponentData(agentEntity, new Translation { Value = transform.position });
            entityManager.SetComponentData(agentEntity, new Rotation { Value = transform.rotation });
        }
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
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;
    }

    public virtual void SetResetParams() { }

    protected virtual void PerformAction(float[] vectorAction) { }

    /// <summary>
    /// An ECS Implementation of The original Mono Form AddReward
    /// </summary>
    public void AddReward_Ecs(float reward)
    {
        var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
        agentComponent.Reward += reward;
        entityManager.SetComponentData(agentEntity, agentComponent);
    }

    /// <summary>
    /// Overriding the SendInfo function so that it ensures the reward in AgenData is reset
    /// </summary>
    public override void SendInfo()
    {
        if (m_RequestDecision)
        {
            if (agentEntity != new Entity())
            {
                var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
                SetReward(agentComponent.Reward);
                base.SendInfo();
                agentComponent.Reward = 0;
                entityManager.SetComponentData(agentEntity, agentComponent);
            }
        }
    }

    public override void NotifyAgentDone(DoneReason doneReason)
    {
        if (agentEntity != new Entity() && doneReason != DoneReason.Disabled)
        {
            var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
            SetReward(agentComponent.Reward);
            //Agent's reward is reset to 0 When Agent is Done
            entityManager.SetComponentData(agentEntity, new AgentData { Reward = 0, teamID = this.teamID, health = this.health });
        }
        base.NotifyAgentDone(doneReason);
    }

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

    public bool IsOpponent(GameObject col)
    {
        if (col.CompareTag("TeamOne") || col.CompareTag("TeamTwo"))
            if (!col.CompareTag(gameObject.tag)) return true;
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

    #region  Mono

    // private void LateUpdate()
    // {
    //     if (agentEntity != new Entity())
    //     {
    //         if (entityManager.HasComponent<AgentData>(agentEntity))
    //         {
    //             var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
    //             if (agentComponent.health <= 0 && Alive)
    //             {
    //                 area.UpdateStatus(gameObject.tag);
    //                 gameObject.tag = "dead";
    //                 Alive = false;
    //                 SetAnimation("isDead");
    //             }
    //             else if (agentComponent.health > 0)
    //             {
    //                 gameObject.tag = teamID == TeamID.TeamOne ? "TeamOne" : "TeamTwo";
    //                 Alive = true;
    //             }
    //         }
    //         //Update Agent's ECSBody
    //         entityManager.SetComponentData(agentEntity, new Translation { Value = transform.position });
    //         entityManager.SetComponentData(agentEntity, new Rotation { Value = transform.rotation });
    //     }
    // }

    private void OnDestroy()
    {
        blobAssetStore.Dispose();
    }

    #endregion

    #region Interface

    /// <summary>
    /// 智能代理受伤调用的函数。代理死亡时返回true (GameObejct交互接口)
    /// </summary>
    /// <param name="damage">遭受的伤害值</param>
    public virtual void Damaged(int damage)
    {
        var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
        agentComponent.health -= damage;
        //agentComponent.Reward += -((float)damage / health);
        entityManager.SetComponentData(agentEntity, agentComponent);
    }

    void IReceiveEntity.SetReceivedEntity(Entity entity)
    {
        agentEntity = entity;
        entityManager.AddComponentData(agentEntity, new AgentData { Reward = 0, teamID = this.teamID, health = this.health });
    }

    #endregion

}
