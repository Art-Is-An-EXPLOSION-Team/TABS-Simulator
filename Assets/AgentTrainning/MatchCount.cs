using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MatchCount : MonoBehaviour
{
    //public static MatchCount Instance;

    public static int TeamOneWinTotal = 0;
    public static int TeamTwoWinTotal = 0;
    public static int TeamOneWin = 0;
    public static int TeamTwoWin = 0;

    static int counter = 0;

    TextMeshProUGUI textOne;
    TextMeshProUGUI textTwo;
    TextMeshProUGUI textThree;
    TextMeshProUGUI textFour;

    private void Awake()
    {
        var texts = GetComponentsInChildren<TextMeshProUGUI>();
        textOne = texts[0];
        textTwo = texts[1];
        textThree = texts[2];
        textFour = texts[3];
    }

    public static void UpdateStatus(TeamID winTeam)
    {
        if (++counter >= 100)
        {
            TeamOneWin = TeamTwoWin = 0;
            counter = 1;
        }

        if (winTeam == TeamID.TeamOne)
        {
            TeamOneWin++;
            TeamOneWinTotal++;
        }
        else
        {
            TeamTwoWin++;
            TeamTwoWinTotal++;
        }
    }

    private void Update()
    {
        textOne.text = "TeamOne wins Total : " + TeamOneWinTotal;
        textTwo.text = "TeamOne wins Current : " + TeamOneWin;
        textThree.text = "TeamTwo wins Total : " + TeamTwoWinTotal;
        textFour.text = "TeamTwo wins Current : " + TeamTwoWin;
    }
}
