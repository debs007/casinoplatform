using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace bigsmall
{
    public class GetPoints : MonoBehaviour
    {
        public static GetPoints getPoints;
        //public Image[] ChangableSprites;
        int ChipValue = 0;
        public bool ChipSelected = false;
        GameObject _GlobalChipHolder;
        public List<int> baitList = new List<int>();
        public TextMeshProUGUI PlayPoints,MainPoints,Wintext;
        /// <summary>True from the end of the betting timer until the next round starts.</summary>
        public static bool Is_Rolling = false;
        public int tokensWon,won_on_number;
        public int multiplication = 9;
       // public GameObject front_msg_text,DoubleTextPlay,DoubleTextWin;
        public RectTransform fmsg_parent;
        Color col;
        Coroutine resultCrt;

        public static string gotNumber;
        [SerializeField]
        public List<int> repeat = new List<int>();
        public Button[] allButtonsToDisable;
        //static Button[] allButtonsToDisableStatic;
        public static bool allowBait = true;

        public Button[] odds;
        public Button[] evens;
        public GameObject[] allNumbersButton;

        public GameObject NumberDrawPanel;
        public Dictionary<string, int> numberValues = new Dictionary<string, int>();
        public int LastBait = 0;
        public TextMeshProUGUI XNumber;
        public Animator NAnim;
        public TextMeshProUGUI NNumber;
        public GameObject[] allToDisable;
        public GameObject youWinMsg;
        public TextMeshProUGUI YouWinText;
        public GameObject Glow;
        public Text MyName;

        public Transform allLightHolder;
        public GameObject BlackLayover;
        public AudioSource winaudio;
        public Image xNum;
        public Sprite[] xNumns;
        public Transform pointValuesText;

        // ---------------- Bet slip ----------------
        // Flow: number clicks only build a local slip (pending). Nothing is sent until the
        // player presses the Bet button (PlaceBet). The request carries the session totals
        // (already accepted + this slip), the same payload updateBet always received, so the
        // backend needs no change and re-sending after a timeout is safe.

        [Header("Bet slip")]
        [Tooltip("The Bet button. OnClick -> GetPoints.PlaceBet. Enabled only when there is something to send.")]
        public Button betButton;

        [Header("Big Small areas")]
        [Tooltip("Shows the unsent chips on Small (total of numbers 2-6). Small button OnClick -> GetPoints.SelectSmall")]
        public TextMeshProUGUI smallBetText;
        [Tooltip("Shows the unsent chips on 7. Seven button OnClick -> GetPoints.SelectSeven")]
        public TextMeshProUGUI sevenBetText;
        [Tooltip("Shows the unsent chips on Big (total of numbers 8-12). Big button OnClick -> GetPoints.SelectBig")]
        public TextMeshProUGUI bigBetText;

        [Header("Dice")]
        [Tooltip("Your CasinoDiceRoller. When the result arrives, Roll(winning number) is called on it. " +
                 "When the dice stop, call GetPoints.getPoints.ShowWinResult().")]
        public CasinoDiceRoller diceRoller;
        [Tooltip("Extra wait before the win is shown after ShowWinResult() is called.")]
        public float winShowDelay = 0f;

        /// <summary>Last result from the server: dice faces (0 until the result arrives).</summary>
        public static int Dice1 { get; private set; }
        public static int Dice2 { get; private set; }
        /// <summary>True once this round's result (number, multiplier, dice) has arrived.</summary>
        public static bool ResultIsReady { get; private set; }
        bool winShown = false;

        /// <summary>Chips placed on the board but not yet sent to the server, indexed by number 2-12.</summary>
        readonly int[] pending = new int[NumberPoints.Max + 1];
        /// <summary>Bets accepted by the server since the last ticket print.</summary>
        readonly int[] unprinted = new int[NumberPoints.Max + 1];
        /// <summary>Text components that show the slip value for each number (if the board has number buttons).</summary>
        readonly List<Text>[] numberTexts = new List<Text>[NumberPoints.Max + 1];

        int serverBalance = 0;   // last balance confirmed by the server
        int serverPlay = 0;      // last play total confirmed by the server for this session
        bool betInFlight = false;
        int roundId = 0;         // bumped every time a round is reset, so late responses can be ignored

        static readonly string[] numberWords = { "", "", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve" };

        public int PendingTotal
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < pending.Length; i++) sum += pending[i];
                return sum;
            }
        }

        public bool HasPendingBets { get { return PendingTotal > 0; } }

        /// <summary>Slip can be edited only while betting is open and no bet request is waiting for a reply.</summary>
        public bool CanEditSlip { get { return allowBait && !betInFlight; } }

        public bool CanPlaceBet { get { return CanEditSlip && GameController.TimerStarted && HasPendingBets; } }

        /// <summary>Balance the player can still put on the slip.</summary>
        int AvailableBalance { get { return serverBalance - PendingTotal; } }

        // Start is called before the first frame update
        void Start()
        {
            getPoints = this;
            col = PlayPoints.color;

            // Statics survive SceneManager reloads (GameController reloads the scene after a long minimise),
            // so start every scene load from a clean round.
            allowBait = true;
            NumberPoints.Reset();

            int parsed;
            if (int.TryParse(MainPoints.text, out parsed)) serverBalance = parsed;
            if (int.TryParse(PlayPoints.text, out parsed)) serverPlay = parsed;

            for (int i = 0; i < numberTexts.Length; i++) numberTexts[i] = new List<Text>();
            if (allNumbersButton != null)
                foreach (GameObject go in allNumbersButton) RegisterNumberText(go);
            if (NumberDrawPanel != null)
                foreach (Transform trn in NumberDrawPanel.transform) RegisterNumberText(trn.gameObject);

    #if UNITY_ANDROID || UNITY_IOS
            foreach(GameObject obj in allToDisable)
            {
                obj.SetActive(false);
            }
    #endif

            //allButtonsToDisableStatic = allButtonsToDisable;


        }

        private void OnGUI()
        {



        }

        public void DisableAll()
        {
            foreach (Button btn in allButtonsToDisable)
            {
                if (allowBait == false)
                    btn.interactable = false;
                else
                    btn.interactable = true;
            }
        }

        // Odds / Evens / Random only add chips to the slip (through GetNumbersBait); nothing is sent until Bet is pressed.
        public void SelectOdds()
        {
            if (!CanEditSlip)
            {
                return;
            }
            foreach(Button btn in odds)
            {
                btn.onClick.Invoke();
            }
        }
        public void SelectEvens()
        {
            if (!CanEditSlip)
            {
                return;
            }
            foreach (Button btn in evens)
            {
                btn.onClick.Invoke();
            }
        }

        public void SelectRandom(int lim)
        {
            if (!CanEditSlip)
            {
                return;
            }
            ClearPendingSlip();
            // Guard: asking for more numbers than exist used to loop forever
            lim = Mathf.Clamp(lim, 0, allNumbersButton.Length);
            List<int> usedNumbers = new List<int>();
            for(int i=0;i< lim;)
            {
                int slectedRand = UnityEngine.Random.Range(0, allNumbersButton.Length);

                if(!usedNumbers.Contains(slectedRand))
                {
                    GameObject btn = allNumbersButton[slectedRand];
                    btn.GetComponent<Button>().onClick.Invoke();
                    usedNumbers.Add(slectedRand);
                    i++;
                }
            }
        }

        public void PrintFromButton()
        {
            PrintTicket();
        }

        /// <summary>Prints the bets the server has accepted since the last print. Unsent slip chips are not printed.</summary>
        void PrintTicket()
        {
            string sessionNum = Constants.sessionData != null ? Constants.sessionData.sessionNum : "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Session:").Append(sessionNum).Append("\nDate-Time:").Append(DateTime.Now.ToString()).Append("\n");
            int total = 0;
            for (int n = NumberPoints.Min; n <= NumberPoints.Max; n++)
            {
                sb.Append("\n").Append(numberWords[n]).Append(" : ").Append(unprinted[n]);
                total += unprinted[n];
            }
            sb.Append("\n-------------\nTotal: ").Append(total);

            string[] data = new string[2];
            data[0] = sb.ToString();
            data[1] = "";
            PrintNow.Instance.StartPrint(data);

            for (int i = 0; i < unprinted.Length; i++) unprinted[i] = 0;
        }

        // Update is called once per frame
        void Update()
        {
            if (betButton != null)
            {
                betButton.interactable = CanPlaceBet;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                PrintTicket();
            }
        }

        private bool ClearVal = false;
        public void AllowNumberClear()
        {
            ClearVal = true;
        }

        /// <summary>"Clear one number" mode: removes that number from the unsent slip.</summary>
        public void ClearNumber(GameObject num)
        {
            if (!CanEditSlip)
            {
                return;
            }
            int n;
            if (!TryGetNumber(num, out n))
            {
                return;
            }
            pending[n] = 0;
            RefreshBetUI();
        }

        /// <summary>Doubles the chips on the unsent slip.</summary>
        public void MakeDouble()
        {
            if (!CanEditSlip)
            {
                return;
            }
            int slip = PendingTotal;
            if (slip <= 0)
            {
                return;
            }
            if (AvailableBalance < slip)
            {
                ScreenFlowHandler.screenFlow.ShowErr();
                return;
            }
            for (int i = 0; i < pending.Length; i++)
            {
                pending[i] *= 2;
            }
            RefreshBetUI();
        }

        public void CancleDouble()
        {
            if(multiplication == 9 && !Is_Rolling)
            {
                multiplication = 9;
               // DoubleTextWin.SetActive(false);
              //  DoubleTextPlay.SetActive(false);
                popupmsg("Cancled");
            }

        }

        IEnumerator GetWinNumber()
        {
            if (winShowDelay > 0f)
            {
                yield return new WaitForSeconds(winShowDelay);
            }

            StartCoroutine(ResultView.resultView.RefreshLeftResultPanel());
            int check = Convert.ToInt32(gotNumber);
            Debug.Log("Checking for: " + check);
            foreach (int n in baitList)
                Debug.Log("The current list is: " + n);
            if (baitList.Contains(check))
            {
                Debug.Log("User won among list");
                StartCoroutine(GameController.instance.Setwin());
                SetWinandLossValues(true);
            }
            else
            {
                SetWinandLossValues(false);
            }
            if(numberFlashLight != null)
            {
                StopCoroutine(numberFlashLight);
            }

            numberFlashLight = StartCoroutine(ShowNumberWinReflect());
            winaudio.Play();
            xNum.gameObject.SetActive(false);

        }


        Coroutine numberFlashLight;
        IEnumerator ShowNumberWinReflect()
        {
            BlackLayover.SetActive(true);
            foreach (Transform trn in allLightHolder)
            {
                trn.gameObject.GetComponent<Image>().color = new Color(255f, 255f, 255f, 1f);
            }
            while (true)
            {
                yield return new WaitForSeconds(.2f);
                foreach(Transform trn in allLightHolder)
                {
                    trn.gameObject.GetComponent<Image>().color = Color.red;
                }
                yield return new WaitForSeconds(.2f);
                foreach (Transform trn in allLightHolder)
                {
                    trn.gameObject.GetComponent<Image>().color = Color.yellow;
                }
                yield return new WaitForSeconds(.2f);
                foreach (Transform trn in allLightHolder)
                {
                    trn.gameObject.GetComponent<Image>().color = Color.green;
                }
            }


        }

        void SetWinandLossValues(bool status)
        {
            // Local estimate only; GameController.Setwin overwrites it with the server balance when it replies.
            int MainPointsNow = serverBalance;
            multiplication *= GameController.selectedXVal;

            int winNumber;
            if (status && int.TryParse(gotNumber, out winNumber) && NumberPoints.IsValid(winNumber))
            {
                tokensWon = NumberPoints.ByNumber[winNumber] * multiplication;
                won_on_number = winNumber;
                MainPointsNow += tokensWon;
            }
            else
            {
                tokensWon = 0;
                won_on_number = -90;
            }

            multiplication = 9;
            //NAnim.enabled = true;
           NAnim.SetBool("Hit", true);
            NNumber.text = gotNumber.ToString();
            NNumber.gameObject.SetActive(true);
            SetServerBalance(MainPointsNow);

            if(status)
            Wintext.text =(Convert.ToInt32(Wintext.text) + tokensWon).ToString();

            GameObject obj = null;
            if (status)
            {
                //obj.GetComponent<Animator>().SetBool("Play", true);

                popupmsg("+" + tokensWon.ToString());


            }
           // DoubleTextWin.SetActive(false);
           // DoubleTextPlay.SetActive(false);


            StartCoroutine(ShowWinOrLossMsg(obj));



        }

        IEnumerator ShowWinOrLossMsg(GameObject obj)
        {
            yield return new WaitForSeconds(1f);
            //popupmsg("-" + (Convert.ToInt32(PlayPoints.text)).ToString());
            if (Wintext.text != "0")
            {
                youWinMsg.SetActive(true);
                YouWinText.text = Wintext.text;
            }
            Glow.SetActive(true);
            yield return new WaitForSeconds(1f);
            GameController.TimerStarted = false;
            yield return new WaitForSeconds(2f);
            Glow.SetActive(false);
            if (obj != null)
            obj.GetComponent<Animator>().SetBool("Play", false);
            multiplication = 9;
            allowBait = true;
            Clear(NumberDrawPanel,true);
            if(numberFlashLight != null)
            {
                StopCoroutine(numberFlashLight);
                numberFlashLight = null;
            }

            foreach(Transform trn in allLightHolder)
            {
                trn.gameObject.GetComponent<Image>().color = new Color(255f, 255f, 255f, 0f);
            }

            BlackLayover.SetActive(false);
            GameController.instance.CheckSession();

        }

        public void popupmsg(string text)
        {
            return;
            //GameObject obj = Instantiate(front_msg_text);
            //obj.GetComponent<RectTransform>().SetParent(fmsg_parent);
            //obj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            //obj.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
            //obj.GetComponent<TextMeshProUGUI>().text = text;
            ////obj.GetComponent<TextMeshProUGUI>().fontSize = 100f;

            //Destroy(obj, 3f);
        }

        public void SetChipValue(GameObject Obj)
        {
            if(_GlobalChipHolder != null && Obj == _GlobalChipHolder)
            {
                return;
            }
            ChipSelected = true;

            ChipValue = Obj.GetComponent<HoldingValues>().Value;
            Obj.transform.parent.GetComponent<Image>().enabled = true;
            if(_GlobalChipHolder != null)
            {
                _GlobalChipHolder.transform.parent.GetComponent<Image>().enabled =false;
            }
            _GlobalChipHolder = Obj;
        }

        /// <summary>Number button click: adds the selected chip to the unsent slip. No API call here.</summary>
        public void GetNumbersBait(GameObject num)
        {
            if (!ChipSelected || !CanEditSlip)
            {
                return;
            }

            if (ClearVal)
            {
                ClearVal = false;
                ClearNumber(num);
                return;
            }

            int n;
            if (!TryGetNumber(num, out n))
            {
                Debug.LogWarning("GetNumbersBait: button name is not a number 0-9: " + (num != null ? num.name : "null"));
                return;
            }
            RegisterNumberText(num);

            if (AvailableBalance < ChipValue)
            {
                ScreenFlowHandler.screenFlow.ShowErr();
                return;
            }

            pending[n] += ChipValue;
            RefreshBetUI();
        }

        /// <summary>Small button: puts the selected chip on EACH of 2, 3, 4, 5, 6 (costs 5 chips).</summary>
        public void SelectSmall()
        {
            AddChipToRange(NumberPoints.SmallFrom, NumberPoints.SmallTo);
        }

        /// <summary>7 button: puts the selected chip on 7 only.</summary>
        public void SelectSeven()
        {
            AddChipToRange(NumberPoints.Seven, NumberPoints.Seven);
        }

        /// <summary>Big button: puts the selected chip on EACH of 8, 9, 10, 11, 12 (costs 5 chips).</summary>
        public void SelectBig()
        {
            AddChipToRange(NumberPoints.BigFrom, NumberPoints.BigTo);
        }

        void AddChipToRange(int from, int to)
        {
            if (!ChipSelected || !CanEditSlip)
            {
                return;
            }

            // "clear one" mode clears the whole area
            if (ClearVal)
            {
                ClearVal = false;
                for (int n = from; n <= to; n++) pending[n] = 0;
                RefreshBetUI();
                return;
            }

            // all or nothing: never put the chip on only part of the range
            int cost = ChipValue * (to - from + 1);
            if (AvailableBalance < cost)
            {
                ScreenFlowHandler.screenFlow.ShowErr();
                return;
            }

            for (int n = from; n <= to; n++)
            {
                pending[n] += ChipValue;
            }
            RefreshBetUI();
        }

        /// <summary>
        /// Called by GameController when getWin answers: stores the number, multiplier and dice faces,
        /// then rolls the dice: diceRoller.Roll(winning number).
        /// </summary>
        public void SetRoundResult(string number, int times, string dice)
        {
            gotNumber = number;
            GameController.selectedXVal = times;

            int n;
            int.TryParse(number, out n);

            int d1, d2;
            if (!ResultView.TryParseDice(dice, out d1, out d2) && NumberPoints.IsValid(n))
            {
                // result without dice: any pair that adds up to the number
                d1 = Mathf.Clamp(n - 1, 1, 6);
                d2 = n - d1;
            }
            // faces the server saved for this result (shown in the history); Roll() can read them
            Dice1 = d1;
            Dice2 = d2;
            ResultIsReady = true;

            if (diceRoller != null)
            {
                diceRoller.Roll(n);
            }
            else
            {
                Debug.LogError("GetPoints: Dice Roller is not assigned, so the dice can't roll. Result was " + number);
            }
        }

        /// <summary>
        /// Call this from your dice-stop event. Shows the win/loss, updates the result history and starts
        /// the next round. Safe to call early: it waits for the result, and calling it twice in a round
        /// does nothing.
        /// </summary>
        public void ShowWinResult()
        {
            if (winShown)
            {
                return;
            }
            winShown = true;
            StartCoroutine(ShowWinWhenReady());
        }

        IEnumerator ShowWinWhenReady()
        {
            while (!ResultIsReady)
            {
                yield return null;
            }

            if (Xcrt != null)
            {
                StopCoroutine(Xcrt);
                Xcrt = null;
            }
            if (GameController.selectedXVal == 0)
            {
                GameController.selectedXVal = 1;
            }
            SetXSprite(GameController.selectedXVal);

            StartCoroutine(GetWinNumber());
        }

        void SetXSprite(int index)
        {
            if (xNum != null && xNumns != null && index >= 0 && index < xNumns.Length)
            {
                xNum.sprite = xNumns[index];
            }
        }

        /// <summary>
        /// Bet button. Sends the whole slip in ONE request. While it is in flight the slip is locked;
        /// on success the board texts are cleared, on failure the slip stays so the player can retry.
        /// </summary>
        public void PlaceBet()
        {
            if (betInFlight)
            {
                return;
            }
            if (!allowBait || !GameController.TimerStarted)
            {
                GameController.instance.ShowMessage("Betting is closed");
                return;
            }
            if (!HasPendingBets)
            {
                return;
            }
            if (AvailableBalance < 0)
            {
                ScreenFlowHandler.screenFlow.ShowErr();
                return;
            }

            int[] slip = (int[])pending.Clone();
            int[] totals = new int[NumberPoints.Max + 1];
            for (int i = 0; i < totals.Length; i++)
            {
                // updateBet expects the session totals per number, not just this slip
                totals[i] = NumberPoints.ByNumber[i] + slip[i];
            }

            string session = Constants.sessionData.session;
            int sentRound = roundId;
            betInFlight = true;
            GameController.instance.ShowMessage("Placing bet...");

            StartCoroutine(GameController.instance.SubmitBets(session, totals,
                (ok, balance, play, error) => OnBetResponse(sentRound, session, slip, totals, ok, balance, play, error)));
        }

        void OnBetResponse(int sentRound, string sentSession, int[] slip, int[] totals, bool ok, int balance, int play, string error)
        {
            betInFlight = false;

            // The round ended (or a new session started) while waiting: nothing on the board belongs to that bet anymore.
            if (sentRound != roundId || Constants.sessionData == null || Constants.sessionData.session != sentSession)
            {
                Debug.LogWarning("Ignoring updateBet response for an old round/session " + sentSession + " ok=" + ok);
                RefreshBetUI();
                return;
            }

            if (ok)
            {
                for (int i = 0; i < totals.Length; i++)
                {
                    NumberPoints.ByNumber[i] = totals[i];
                    unprinted[i] += slip[i];
                    pending[i] = 0;          // clears the bet texts on the board
                }
                serverBalance = balance;
                serverPlay = play;
                RebuildBaitList();
                RefreshBetUI();
                GameController.instance.ShowMessage("Bet placed");
                return;
            }

            Debug.LogError("updateBet failed: " + error);
            if (!allowBait)
            {
                // Too late to retry - give the chips back.
                ClearPendingSlip();
                GameController.instance.ShowMessage("Bet not placed - betting closed");
            }
            else
            {
                RefreshBetUI();
                GameController.instance.ShowMessage("Bet not placed. Please press Bet again");
            }
        }

        /// <summary>
        /// Called when betting closes. Chips that were never sent are dropped and the balance shown is restored.
        /// A bet already in flight is left alone; its response decides what happens.
        /// </summary>
        public void DiscardPendingBets(string reason)
        {
            if (betInFlight || !HasPendingBets)
            {
                return;
            }
            ClearPendingSlip();
            if (!string.IsNullOrEmpty(reason))
            {
                GameController.instance.ShowMessage(reason);
            }
        }

        /// <summary>Authoritative balance from the server (login, updateBet, setWinValue).</summary>
        public void SetServerBalance(int balance)
        {
            serverBalance = balance;
            RefreshBalanceTexts();
        }

        void ClearPendingSlip()
        {
            for (int i = 0; i < pending.Length; i++)
            {
                pending[i] = 0;
            }
            RefreshBetUI();
        }

        /// <summary>End of round: forget the slip and the session totals.</summary>
        void ResetForNewRound()
        {
            roundId++;
            for (int i = 0; i < pending.Length; i++)
            {
                pending[i] = 0;
                unprinted[i] = 0;
            }
            NumberPoints.Reset();
            serverPlay = 0;
            baitList.Clear();
            RefreshBetUI();
        }

        void RebuildBaitList()
        {
            baitList.Clear();
            for (int i = 0; i < NumberPoints.ByNumber.Length; i++)
            {
                if (NumberPoints.ByNumber[i] > 0)
                {
                    baitList.Add(i);
                }
            }
        }

        void RefreshBetUI()
        {
            for (int i = 0; i < numberTexts.Length; i++)
            {
                if (numberTexts[i] == null) continue;
                foreach (Text t in numberTexts[i])
                {
                    if (t != null) t.text = pending[i].ToString();
                }
            }
            if (smallBetText != null) smallBetText.text = PendingOn(NumberPoints.SmallFrom, NumberPoints.SmallTo).ToString();
            if (sevenBetText != null) sevenBetText.text = PendingOn(NumberPoints.Seven, NumberPoints.Seven).ToString();
            if (bigBetText != null) bigBetText.text = PendingOn(NumberPoints.BigFrom, NumberPoints.BigTo).ToString();
            RefreshBalanceTexts();
        }

        /// <summary>Unsent chips on numbers from..to (e.g. 2..6 = Small).</summary>
        public int PendingOn(int from, int to)
        {
            int sum = 0;
            for (int n = from; n <= to; n++) sum += pending[n];
            return sum;
        }

        void RefreshBalanceTexts()
        {
            MainPoints.text = (serverBalance - PendingTotal).ToString();
            PlayPoints.text = (serverPlay + PendingTotal).ToString();
        }

        static bool TryGetNumber(GameObject go, out int n)
        {
            n = -1;
            return go != null && int.TryParse(go.name, out n) && NumberPoints.IsValid(n);
        }

        /// <summary>Remembers the value Text of a number button (child 0 -> child 0, same as the original code).</summary>
        void RegisterNumberText(GameObject go)
        {
            int n;
            if (!TryGetNumber(go, out n)) return;
            Transform t = go.transform;
            if (t.childCount == 0 || t.GetChild(0).childCount == 0) return;
            Text txt = t.GetChild(0).GetChild(0).GetComponent<Text>();
            if (txt == null) return;
            if (numberTexts[n] == null) numberTexts[n] = new List<Text>();
            if (!numberTexts[n].Contains(txt)) numberTexts[n].Add(txt);
        }

        Coroutine Xcrt = null;

        /// <summary>
        /// Called by GameController when the betting timer runs out. The result is fetched next,
        /// and SetRoundResult() rolls the dice with it.
        /// </summary>
        public void OnTimerEnded()
        {
            if(!Is_Rolling && GameController.has_network)
            {
                // Safety net: anything still on the slip now was never sent, so it isn't in play.
                DiscardPendingBets("Unplaced bets removed");
                Is_Rolling = true;
                ResultIsReady = false;
                winShown = false;
                Dice1 = 0; Dice2 = 0;
                Xcrt = StartCoroutine(ShowXVal());
            }
        }

        public IEnumerator ShowXVal()
        {
            int counter = 1;
            xNum.gameObject.SetActive(true);
            while(Is_Rolling)
            {
                yield return new WaitForSeconds(.3f);

                if (counter == 1)
                {
                    //XNumber.text = "N";
                    xNum.sprite = xNumns[counter];
                }
                else
                {
                    //XNumber.text = counter + "X";
                    xNum.sprite = xNumns[counter];

                }

                if(counter == 5)
                {
                    counter = 0;
                }

                counter++;

                //waitTime += 0.05f;
            }
        }

        /// <summary>Clear button: empties the unsent slip. Bets already sent are not touched.</summary>
        public void CLR(GameObject Traverse)
        {
            if (!CanEditSlip)
            {
                return;
            }
            ClearPendingSlip();

            multiplication = 9;
          //  DoubleTextWin.SetActive(false);
    //        DoubleTextPlay.SetActive(false);

            GameController.instance.CheckRepeat();
        }

        /// <summary>
        /// onlyClear = true  -> end of round: reset slip + session totals (called from ShowWinOrLossMsg).
        /// onlyClear = false -> just empty the unsent slip.
        /// </summary>
        public void Clear(GameObject Traverse, bool onlyClear = false, bool SpecialAccess = false)
        {
            if (!allowBait && !SpecialAccess)
            {
                return;
            }

            if (onlyClear)
            {
                ResetForNewRound();
            }
            else if (!betInFlight)
            {
                ClearPendingSlip();
            }

            multiplication = 9;
          //  DoubleTextWin.SetActive(false);
          //  DoubleTextPlay.SetActive(false);
        }

        /// <summary>Repeat: puts last round's bets on the slip (replaces the current slip, so double-clicking doesn't double it).</summary>
        public void Repeat()
        {
            if(!CanEditSlip)
            {
                return;
            }

            int[] last = new int[NumberPoints.Max + 1];
            int lastTotal = 0;
            foreach(KeyValuePair<string,int> numVal in numberValues)
            {
                int n;
                if (int.TryParse(numVal.Key, out n) && NumberPoints.IsValid(n) && numVal.Value > 0)
                {
                    last[n] = numVal.Value;
                    lastTotal += numVal.Value;
                }
            }
            if (lastTotal <= 0)
            {
                return;
            }

            // current slip is replaced, so only the confirmed balance matters here
            if (serverBalance < lastTotal)
            {
                ScreenFlowHandler.screenFlow.ShowErr();
                return;
            }

            for (int i = 0; i < pending.Length; i++)
            {
                pending[i] = last[i];
            }
            RefreshBetUI();
        }

        /// <summary>Right click on a number: removes one chip of the selected value from the unsent slip.</summary>
        public void ClearParticularObject(GameObject obj)
        {
            if (!CanEditSlip)
            {
                return;
            }
            int n;
            if (!TryGetNumber(obj, out n) || pending[n] <= 0)
            {
                return;
            }
            int remove = (ChipSelected && ChipValue > 0) ? ChipValue : pending[n];
            pending[n] = Mathf.Max(0, pending[n] - remove);
            RefreshBetUI();
        }
    }
}
