using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MLAgents;
public class CumulativeScript : MonoBehaviour
{
    public Agent agent;
    public TextMeshPro textMeshPro;
    // Update is called once per frame
    void Update()
    {
        textMeshPro.text = "CumulativeReward: " + agent.GetCumulativeReward().ToString();
    }
}
