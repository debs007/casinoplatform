using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace TwelveCards
{
    public class CoinMainDetect : MonoBehaviour
    {
        public Text myText;
        public Text valueText;

        private void Update()
        {
            if (Convert.ToInt32(valueText.text) > 0)
            {
                int val = Convert.ToInt32(valueText.text);
                // print("val " + val);
                this.GetComponent<Image>().color = new Color(255f, 255f, 255f, 1.0f);
                myText.color = new Color(0f, 0f, 0f, 1.0f);
                myText.text = valueText.text;

                if (val >= 1000)
                {
                    //print("val " + val);
                    this.GetComponent<Image>().sprite = ScreenFlowHandler.instance.sprite1000;
                }
                else if (val >= 500)
                {
                    // print("val " + val);
                    this.GetComponent<Image>().sprite = ScreenFlowHandler.instance.sprite500;
                }
                else if (val >= 100)
                {
                    //print("val " + val);
                    this.GetComponent<Image>().sprite = ScreenFlowHandler.instance.sprite100;
                }
                else if (val >= 50)
                {
                    //print("val " + val);
                    this.GetComponent<Image>().sprite = ScreenFlowHandler.instance.sprite50;
                }
                else if (val >= 10)
                {
                    // print("val " + val);
                    this.GetComponent<Image>().sprite = ScreenFlowHandler.instance.sprite10;
                }
                else if (val >= 5)
                {
                    // print("val " + val);
                    this.GetComponent<Image>().sprite = ScreenFlowHandler.instance.sprite5;
                }
            }
            else
            {
                this.GetComponent<Image>().color = new Color(255f, 255f, 255f, 0f);
                myText.color = new Color(0f, 0f, 0f, 0f);
                // print("valuee ");
            }
        }
    }
}