using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace bigsmall
{
    public class ResultView : MonoBehaviour
    {
        public Transform LeftResultPanel;
        public static ResultView resultView;
        Coroutine crt;
        [Tooltip("Row colour per number: element 0 = number 2 ... element 10 = number 12")]
        public Color[] allColors;
        [SerializeField]
        public Dictionary<string, Color> ColorPair = new Dictionary<string, Color>();

        [Header("Big Small: dice in the result history")]
        [Tooltip("Dice face sprites: element 0 = face 1 ... element 5 = face 6. " +
                 "Give each history row two Image children named \"Dice1\" and \"Dice2\" and they are filled automatically.")]
        public Sprite[] diceFaces;

        /// <summary>One finished round, newest first in History.</summary>
        [Serializable]
        public class RoundResult
        {
            public int number;
            public int dice1;
            public int dice2;
            public int times;
        }

        /// <summary>Recent results (newest first) with dice faces, for your own history UI.</summary>
        public static readonly List<RoundResult> History = new List<RoundResult>();

        /// <summary>Raised whenever History changes (session start, and each new result).</summary>
        public static event Action HistoryChanged;

        private void Awake()
        {
            for (int n = NumberPoints.Min; n <= NumberPoints.Max; n++)
            {
                int i = n - NumberPoints.Min;
                ColorPair[n.ToString()] = (allColors != null && i < allColors.Length) ? allColors[i] : Color.white;
            }

            resultView = this;
        }

        /// <summary>"2,4" -> 2 and 4. False if missing or not two faces 1-6.</summary>
        public static bool TryParseDice(string dice, out int dice1, out int dice2)
        {
            dice1 = 0;
            dice2 = 0;
            if (string.IsNullOrEmpty(dice)) return false;
            string[] parts = dice.Split(',');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0].Trim(), out dice1) || !int.TryParse(parts[1].Trim(), out dice2)) return false;
            return dice1 >= 1 && dice1 <= 6 && dice2 >= 1 && dice2 <= 6;
        }

        public IEnumerator RefreshLeftResultPanel()
        {
            // this round's result, captured now: the rows below animate over several frames
            string gotNumber = GetPoints.gotNumber;
            int selectedXVal = GameController.selectedXVal;
            int dice1 = GetPoints.Dice1, dice2 = GetPoints.Dice2;

            int rows = LeftResultPanel.childCount;
            for(int i = rows - 1; i > 0; i--)
            {
                string lastVal = LeftResultPanel.GetChild(i - 1).GetChild(0).GetComponent<Text>().text;
                Sprite lastSprite = LeftResultPanel.GetChild(i - 1).GetChild(1).GetComponent<Image>().sprite;
                LeftResultPanel.GetChild(i).GetChild(0).GetComponent<Text>().text = lastVal;
                LeftResultPanel.GetChild(i).GetChild(1).GetComponent<Image>().sprite = lastSprite;

                if(LeftResultPanel.GetChild(i - 1).GetChild(1).gameObject.activeSelf)
                {
                    LeftResultPanel.GetChild(i).GetChild(1).gameObject.SetActive(true);
                }
                else
                {
                    LeftResultPanel.GetChild(i).GetChild(1).gameObject.SetActive(false);
                }

                LeftResultPanel.GetChild(i).gameObject.GetComponent<Image>().color =
                    LeftResultPanel.GetChild(i - 1).gameObject.GetComponent<Image>().color;

                yield return null;
            }

            Color colour;
            if (ColorPair.TryGetValue(gotNumber, out colour))
                LeftResultPanel.GetChild(0).gameObject.GetComponent<Image>().color = colour;

            Sprite xSprite = XSprite(selectedXVal);
            if (selectedXVal <= 1 || xSprite == null)
            {
                LeftResultPanel.GetChild(0).GetChild(0).GetComponent<Text>().text = gotNumber;
                LeftResultPanel.GetChild(0).GetChild(1).gameObject.SetActive(false);
            }
            else
            {
                LeftResultPanel.GetChild(0).GetChild(0).GetComponent<Text>().text = "<size=18>" + gotNumber + "</size>\n";
                LeftResultPanel.GetChild(0).GetChild(1).GetComponent<Image>().sprite = xSprite;
                LeftResultPanel.GetChild(0).GetChild(1).gameObject.SetActive(true);
            }

            int number;
            int.TryParse(gotNumber, out number);
            History.Insert(0, new RoundResult { number = number, dice1 = dice1, dice2 = dice2, times = selectedXVal });
            while (History.Count > Mathf.Max(rows, 10)) History.RemoveAt(History.Count - 1);
            ShowDiceInRows();
            if (HistoryChanged != null) HistoryChanged();
        }

        public void VewingMainResult()
        {
            if(crt!=null)
            {
                StopCoroutine(crt);
            }
            if(ServerConnector.ResultView != "")
            {
                StartCoroutine(ServerConnector.serverConnector.Result());
            }
        }


        public void populateResult(Prevbets[] prevbets)
        {
            History.Clear();
            for(int i=0; i<prevbets.Length && i < LeftResultPanel.childCount; i++)
            {
                int times;
                int.TryParse(prevbets[i].times, out times);
                Sprite xSprite = XSprite(times);
                if (times != 0 && xSprite != null)
                {
                    LeftResultPanel.GetChild(i).GetChild(0).GetComponent<Text>().text = "<size=18>" + prevbets[i].number + "</size>\n";
                    LeftResultPanel.GetChild(i).GetChild(1).GetComponent<Image>().sprite = xSprite;
                    LeftResultPanel.GetChild(i).GetChild(1).gameObject.SetActive(true);
                }
                else
                {
                    LeftResultPanel.GetChild(i).GetChild(0).GetComponent<Text>().text = prevbets[i].number;
                    LeftResultPanel.GetChild(i).GetChild(1).gameObject.SetActive(false);
                }

                Color colour;
                if (ColorPair.TryGetValue(prevbets[i].number, out colour))
                    LeftResultPanel.GetChild(i).gameObject.GetComponent<Image>().color = colour;

                int number, d1, d2;
                int.TryParse(prevbets[i].number, out number);
                TryParseDice(prevbets[i].dice, out d1, out d2);
                History.Add(new RoundResult { number = number, dice1 = d1, dice2 = d2, times = times });
            }
            ShowDiceInRows();
            if (HistoryChanged != null) HistoryChanged();
        }

        /// <summary>Fills optional "Dice1"/"Dice2" images in each history row from History.</summary>
        void ShowDiceInRows()
        {
            if (diceFaces == null || diceFaces.Length < 6) return;
            for (int i = 0; i < LeftResultPanel.childCount; i++)
            {
                Transform row = LeftResultPanel.GetChild(i);
                RoundResult r = i < History.Count ? History[i] : null;
                SetDie(row.Find("Dice1"), r != null ? r.dice1 : 0);
                SetDie(row.Find("Dice2"), r != null ? r.dice2 : 0);
            }
        }

        void SetDie(Transform die, int face)
        {
            if (die == null) return;
            Image img = die.GetComponent<Image>();
            if (img == null) return;
            bool valid = face >= 1 && face <= 6;
            if (valid) img.sprite = diceFaces[face - 1];
            die.gameObject.SetActive(valid);
        }

        static Sprite XSprite(int times)
        {
            Sprite[] sprites = GetPoints.getPoints != null ? GetPoints.getPoints.xNumns : null;
            if (sprites == null || times < 0 || times >= sprites.Length) return null;
            return sprites[times];
        }
    }
}
