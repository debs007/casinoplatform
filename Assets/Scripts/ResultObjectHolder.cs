using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace TwelveCards
{
    public class ResultObjectHolder : MonoBehaviour
    {

        public Image Card;
        public Image symbol;
        public Text time;
        public TextMeshProUGUI xVal;

        public string _type, _tem, _times;
        public void Prepare(string type, string tem, string times = "0")
        {
            xVal.gameObject.SetActive(false);
            Debug.LogError("The type : " + type);
            if (string.IsNullOrEmpty(type))
            {
                return;
            }
            int number = Convert.ToInt32(type);
            string[] vals = Constants.cards[number].Split(' ');

            Debug.LogError("Length array :" + ResultView.CardGet.Count + "/" + ResultView.symbolGet.Count);

            Card.sprite = ResultView.CardGet[vals[0]];
            symbol.sprite = ResultView.symbolGet[vals[1]];

            // time.text = tem;
            _type = type;
            _times = times;

            _tem = tem;
            string[] allTem = tem.Split(':');
            time.text = allTem[0] + ":" + allTem[1];

            if (times != "0")
            {
                xVal.text = times + "X";
                xVal.gameObject.SetActive(true);
            }
        }

        public void Repopulate()
        {
            Prepare(_type, _tem, _times);
        }
    }
}
