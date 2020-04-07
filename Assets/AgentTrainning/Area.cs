using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using TMPro;
using Unity.Entities;

public class Area : MonoBehaviour
{

    [Header("Properties")]
    public GameObject AgentTeamOne;
    public GameObject AgentTeamTwo;

    public float range = 19f;
    public bool randomGenerate = true;
    public int numsOfTeamOne_Fixed;
    public int numsOfTeamTwo_Fixed;

    [SerializeField]
    private int m_numsOfAliveTeamOne = 0;
    [SerializeField]
    private int m_numsOfAliveTeamTwo = 0;

    private int m_numsOfTeamOne = 0;
    private int m_numsOfTeamTwo = 0;

    private List<GameObject> agentsList = new List<GameObject>();

    /// <summary>
    /// 当有代理死亡时更新Area状态
    /// </summary>
    public void UpdateStatus(string tag)
    {
        if (tag == "TeamOne")
            m_numsOfAliveTeamOne--;
        else
            m_numsOfAliveTeamTwo--;
        CheckAreaDone();
    }

    public void CheckAreaDone()
    {
        if (m_numsOfAliveTeamOne == 0 || m_numsOfAliveTeamTwo == 0)
        {
            bool rewardFlag = false;
            TeamID winner = TeamID.TeamOne;

            if (!(m_numsOfAliveTeamOne == 0 && m_numsOfAliveTeamTwo == 0))//非平局情况下给予奖励
            {
                rewardFlag = true;
                winner = m_numsOfAliveTeamOne == 0 ? TeamID.TeamTwo : TeamID.TeamOne;
            }

            foreach (GameObject o in agentsList)
            {
                var agentScript = o.GetComponent<AgentECS>();
                if (rewardFlag)
                {
                    if (winner == agentScript.teamID)
                    {
                        agentScript.AddReward_Ecs(1f);
                    }
                    else
                        agentScript.AddReward_Ecs(-1f);
                }
                agentScript.EndEpisode();
            }
        }
    }

    /// <summary>
    /// Reset the area, agent placement
    /// </summary>
    public void ResetArea()
    {
        RemoveAllAgents();
        PlaceAgents();
    }

    public void OnEpisodeBegin()
    {
        m_numsOfAliveTeamOne = m_numsOfTeamOne;
        m_numsOfAliveTeamTwo = m_numsOfTeamTwo;
    }

    /// <summary>
    /// Place the penguin in the area
    /// </summary>
    private void PlaceAgents()
    {
        if (randomGenerate)
        {
            var num = Random.Range(3, 7);
            m_numsOfAliveTeamOne = m_numsOfTeamOne = num;
            m_numsOfAliveTeamTwo = m_numsOfTeamTwo = num;
        }
        else
        {
            m_numsOfAliveTeamOne = m_numsOfTeamOne = numsOfTeamOne_Fixed;
            m_numsOfAliveTeamTwo = m_numsOfTeamTwo = numsOfTeamTwo_Fixed;
        }

        GameObject agentObject;

        for (int i = 0; i < m_numsOfAliveTeamOne + m_numsOfAliveTeamTwo; i++)
        {
            if (i < m_numsOfAliveTeamOne)
            {
                agentObject = Instantiate<GameObject>(AgentTeamOne.gameObject, Vector3.zero, Quaternion.identity, transform);
                agentObject.name = AgentTeamOne.name + i.ToString();
            }
            else
            {
                agentObject = Instantiate<GameObject>(AgentTeamTwo.gameObject, Vector3.zero, Quaternion.identity, transform);
                agentObject.name = AgentTeamTwo.name + (i - m_numsOfAliveTeamOne).ToString();
            }
            // Rigidbody rigidbody = agentObject.GetComponent<Rigidbody>();
            // rigidbody.velocity = Vector3.zero;
            // rigidbody.angularVelocity = Vector3.zero;
            agentsList.Add(agentObject);
        }
    }

    #region Utils

    public Vector3 GetSpawnPos(TeamID teamID)
    {
        if (teamID == TeamID.TeamOne)
            return new Vector3(Random.Range(-range, range), 1f, Random.Range(0, range)) + transform.position + 0.5f * Vector3.up;

        else
            return new Vector3(Random.Range(-range, range), 1f, Random.Range(-range, 0)) + transform.position + 0.5f * Vector3.up;
    }

    private void RemoveAllAgents()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (agentsList != null)
        {
            for (int i = 0; i < agentsList.Count; i++)
                if (agentsList[i] != null)
                    Destroy(agentsList[i]);

        }
        agentsList = new List<GameObject>();
    }

    /// <summary>
    /// Choose a random position on the X-Z plane within a partial donut shape
    /// </summary>
    /// <param name="center">The center of the donut</param>
    /// <param name="minAngle">Minimum angle of the wedge</param>
    /// <param name="maxAngle">Maximum angle of the wedge</param>
    /// <param name="minRadius">Minimum distance from the center</param>
    /// <param name="maxRadius">Maximum distance from the center</param>
    /// <returns>A position falling within the specified region</returns>
    public static Vector3 ChooseRandomPosition(Vector3 center, float minAngle, float maxAngle, float minRadius, float maxRadius)
    {
        float radius = minRadius;
        float angle = minAngle;

        if (maxRadius > minRadius)
        {
            // Pick a random radius
            radius = UnityEngine.Random.Range(minRadius, maxRadius);
        }

        if (maxAngle > minAngle)
        {
            // Pick a random angle
            angle = UnityEngine.Random.Range(minAngle, maxAngle);
        }

        // Center position + forward vector rotated around the Y axis by "angle" degrees, multiplies by "radius"
        return center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
    }

    #endregion

    public void Start()
    {

    }
}