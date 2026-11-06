using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveCards
{
    public class PlayPointsHolde : MonoBehaviour
    {
        public Text Playpoints;

        private void OnGUI()
        {
            Playpoints.text = GetPoints.getPoints.GetTotalOfCurrentBet().ToString();
        }
    }
}