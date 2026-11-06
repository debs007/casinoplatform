using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveCards
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
        public Sprite[] CardSprites;
        public Sprite[] symbolSprites;

        public static Dictionary<string, Sprite> symbolGet = new Dictionary<string, Sprite>();
        public static Dictionary<string, Sprite> CardGet = new Dictionary<string, Sprite>();

        private void Awake()
        {

            if (CardGet.Count <= 0 && symbolGet.Count <= 0)
            {
                Debug.LogError("Setting card get data");
                CardGet.Add("J", CardSprites[0]);
                CardGet.Add("K", CardSprites[1]);
                CardGet.Add("Q", CardSprites[2]);

                symbolGet.Add("heart", symbolSprites[0]);
                symbolGet.Add("spades", symbolSprites[1]);
                symbolGet.Add("diamond", symbolSprites[2]);
                symbolGet.Add("club", symbolSprites[3]);

            }


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


                string lastCard = LeftResultPanel.GetChild(i - 1).GetComponent<ResultObjectHolder>()._type;
                string lastTime = LeftResultPanel.GetChild(i - 1).GetComponent<ResultObjectHolder>()._tem;

                LeftResultPanel.GetChild(i).GetComponent<ResultObjectHolder>().Prepare(lastCard, lastTime);

                //Debug.Log("Last color text " + lastVal);





                yield return null;
            }

            //LeftResultPanel.GetChild(0).gameObject.GetComponent<Image>().color = ColorPair[GetPoints.gotNumber];


            if (GameController.selectedXVal == 1)
            {
                LeftResultPanel.GetChild(0).GetComponent<ResultObjectHolder>().Prepare(GetPoints.gotNumber, GetPoints.gotTime, GameController.selectedXVal.ToString());
            }
            else
            {
                LeftResultPanel.GetChild(0).GetComponent<ResultObjectHolder>().Prepare(GetPoints.gotNumber, GetPoints.gotTime);

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
                    //LeftResultPanel.GetChild(i).GetChild(0).GetComponent<Text>().text = "<size=18>" + prevbets[i].number + "</size>\n";
                    //LeftResultPanel.GetChild(i).GetChild(1).GetComponent<Image>().sprite = GetPoints.getPoints.xNumns[int.Parse(prevbets[i].times)];
                    //LeftResultPanel.GetChild(i).GetChild(1).gameObject.SetActive(true);

                    LeftResultPanel.GetChild(i).GetComponent<ResultObjectHolder>().Prepare(prevbets[i].number, prevbets[i].time, prevbets[i].times);
                }
                else
                    LeftResultPanel.GetChild(i).GetComponent<ResultObjectHolder>().Prepare(prevbets[i].number, prevbets[i].time);

                //LeftResultPanel.GetChild(i).gameObject.GetComponent<Image>().color = ColorPair[prevbets[i].number];
            }

            if (FirstTime)
            {
                FirstTime = false;
                Vector3 lastNumberPos = GetPoints.getPoints.NumbersPositions[int.Parse(prevbets[0].number)];

                GetPoints.getPoints.NumberPanel.transform.rotation = Quaternion.Euler(lastNumberPos);
                GetPoints.getPoints.NumberPanel2.transform.rotation = Quaternion.Euler(GetPoints.getPoints.SymbolPosition[int.Parse(prevbets[0].number)]);
            }
        }
    }
}