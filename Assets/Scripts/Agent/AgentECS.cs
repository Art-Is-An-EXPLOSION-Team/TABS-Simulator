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

    new protected Rigidbody rigidbody;
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
        rigidbody = GetComponent<Rigidbody>();
        rigidbody.maxAngularVelocity = 500;
        m_Aniamtor = GetComponent<Animator>();

        blobAssetStore = new BlobAssetStore();
    }


    /// <summary>
    /// An ECS Implementation of The original Mono Form AddReward
    /// </summary>
    public void AddReward_Ecs(float reward, bool outside = false)
    {
        var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
        agentComponent.Reward += reward;
        entityManager.SetComponentData(agentEntity, agentComponent);

        //When the signal came from outside,make sure the reward is simultaneously added
        if (outside) SetReward(agentComponent.Reward);
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
                agentComponent.Reward = 0;
                entityManager.SetComponentData(agentEntity, agentComponent);
            }
        }
        base.SendInfo();
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

    private void LateUpdate()
    {
        if (agentEntity != new Entity())
        {
            if (entityManager.HasComponent<AgentData>(agentEntity))
            {
                var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
                if (agentComponent.health <= 0 && Alive)
                {
                    area.UpdateStatus(gameObject.tag);
                    gameObject.tag = "dead";
                    Alive = false;
                    SetAnimation("isDead");

                    //Update Agent's reward
                    SetReward(agentComponent.Reward);
                }
                else if (agentComponent.health > 0)
                {
                    gameObject.tag = teamID == TeamID.TeamOne ? "TeamOne" : "TeamTwo";
                    Alive = true;

                    //Update Agent's reward
                    SetReward(agentComponent.Reward);
                }
            }
            //Update Agent's ECSBody
            entityManager.SetComponentData(agentEntity, new Translation { Value = transform.position });
            entityManager.SetComponentData(agentEntity, new Rotation { Value = transform.rotation });
        }
    }

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
    public void Damaged(int damage)
    {
        var agentComponent = entityManager.GetComponentData<AgentData>(agentEntity);
        agentComponent.health -= damage;
        entityManager.SetComponentData(agentEntity, agentComponent);
        AddReward_Ecs(-((float)damage / health));
    }

    void IReceiveEntity.SetReceivedEntity(Entity entity)
    {
        agentEntity = entity;
        entityManager.AddComponentData(agentEntity, new AgentData { Reward = 0, teamID = this.teamID, health = this.health });
    }

    #endregion

}
