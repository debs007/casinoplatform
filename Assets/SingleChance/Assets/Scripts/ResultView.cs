using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SingleChance
{
    public class ResultView : MonoBehaviour
    {
        public Transform LeftResultPanel;
        public static ResultView resultView;
        Coroutine crt;
        public Color[] allColors;
        [SerializeField]
        public Dictionary<string, Color> ColorPair = new Dictionary<string, Color>();
        public GameObject NumberPanel;
        public Vector3[] NumberPositions;
        bool FirstTime = true;

        private void Awake()
        {

            for (int i = 0; i < 10; i++)
            {
                ColorPair.Add(i.ToString(), allColors[i]);
            }

            resultView = this;
            FirstTime = true;
        }
        public IEnumerator RefreshLeftResultPanel()
        {
            for (int i = 9; i > 0; i--)
            {
                string lastVal = LeftResultPanel.GetChild(i - 1).GetChild(0).GetComponent<Text>().text;
                Sprite lastSprite = LeftResultPanel.GetChild(i - 1).GetChild(1).GetComponent<Image>().sprite;
                LeftResultPanel.GetChild(i).GetChild(0).GetComponent<Text>().text = lastVal;
                LeftResultPanel.GetChild(i).GetChild(1).GetComponent<Image>().sprite = lastSprite;

                if (LeftResultPanel.GetChild(i - 1).GetChild(1).gameObject.activeSelf)
                {
                    LeftResultPanel.GetChild(i).GetChild(1).gameObject.SetActive(true);
                }
                else
                {
                    LeftResultPanel.GetChild(i).GetChild(1).gameObject.SetActive(false);
                }

                Debug.Log("Last color text " + lastVal);

                if (ColorPair.ContainsKey(lastVal))
                    LeftResultPanel.GetChild(i).gameObject.GetComponent<Image>().color = ColorPair[lastVal];



                yield return null;
            }

            LeftResultPanel.GetChild(0).gameObject.GetComponent<Image>().color = ColorPair[GetPoints.gotNumber];


            if (GameController.selectedXVal == 1)
            {
                LeftResultPanel.GetChild(0).GetChild(0).GetComponent<Text>().text = GetPoints.gotNumber;
                LeftResultPanel.GetChild(0).GetChild(1).gameObject.SetActive(false);
            }
            else
            {
                LeftResultPanel.GetChild(0).GetChild(0).GetComponent<Text>().text = "<size=18>" + GetPoints.gotNumber + "</size>\n";
                LeftResultPanel.GetChild(0).GetChild(1).GetComponent<Image>().sprite = GetPoints.getPoints.xNumns[GameController.selectedXVal];
                LeftResultPanel.GetChild(0).GetChild(1).gameObject.SetActive(true);

            }

        }

        public void VewingMainResult()
        {
            if (crt != null)
            {
                StopCoroutine(crt);
            }
            if (ServerConnector.ResultView != "")
            {
                StartCoroutine(ServerConnector.serverConnector.Result());
            }
        }


        public void populateResult(Prevbets[] prevbets)
        {
            for (int i = 0; i < prevbets.Length; i++)
            {
                if (prevbets[i].times != "0")
                {
                    LeftResultPanel.GetChild(i).GetChild(0).GetComponent<Text>().text = "<size=18>" + prevbets[i].number + "</size>\n";
                    LeftResultPanel.GetChild(i).GetChild(1).GetComponent<Image>().sprite = GetPoints.getPoints.xNumns[int.Parse(prevbets[i].times)];
                    LeftResultPanel.GetChild(i).GetChild(1).gameObject.SetActive(true);
                }
                else
                    LeftResultPanel.GetChild(i).GetChild(0).GetComponent<Text>().text = prevbets[i].number;

                LeftResultPanel.GetChild(i).gameObject.GetComponent<Image>().color = ColorPair[prevbets[i].number];
            }

            if (FirstTime)
            {
                FirstTime = false;
                Vector3 lastNumberPos = NumberPositions[int.Parse(prevbets[0].number)];

                NumberPanel.transform.rotation = Quaternion.Euler(lastNumberPos);
            }
        }
    }
}
