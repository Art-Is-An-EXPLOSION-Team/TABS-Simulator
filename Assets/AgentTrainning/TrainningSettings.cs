using UnityEngine;
using MLAgents;

public class TrainningSettings : MonoBehaviour
{
    [HideInInspector]
    public Area[] areasList;
    public void Awake() => Academy.Instance.OnEnvironmentReset += EnvironmentReset;

    public void EnvironmentReset()
    {
        areasList = FindObjectsOfType<Area>();
        foreach (var area in areasList)
        {
            area.ResetArea();
        }
    }

}
