using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace SingleChance
{
    public class GetPoints : MonoBehaviour
    {
        public static GetPoints getPoints;
        //public Image[] ChangableSprites;
        int ChipValue = 0;
        private Quaternion _targetRotation = Quaternion.identity;
        public DetectNumber detect;
        public Vector3[] NumbersPositions;
        public bool AllowRotation = false, ChipSelected = false, SetPoisitionNow = false;
        public GameObject NumberPanel;
        GameObject _GlobalChipHolder;
        public List<int> baitList = new List<int>();
        [SerializeField]
        [Range(0f, 10000f)]
        float _Speed = 500f;
        float _SpeedTs = 90f;
        public GameObject BaitNumberPanelParent;
        float time = 0;
        public Text PlayPoints, MainPoints, Wintext;
        Coroutine InstanceHolder;
        bool WinDeclared = false;
        public static bool Is_Spinning = false;
        public int tokensWon, won_on_number;
        public int multiplication = 9;
        public GameObject front_msg_text, DoubleTextPlay, DoubleTextWin;
        public RectTransform fmsg_parent;
        Color col;
        Coroutine resultCrt;
        float startRotation = 0;
        float endRotation = 0;

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
        public Text YouWinText;
        public GameObject Glow;
        public Text MyName;

        public Transform allLightHolder;
        public GameObject BlackLayover;
        public AudioSource SpinWheelAudio, winaudio;
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

        /// <summary>Chips placed on the board but not yet sent to the server (shown on the number texts).</summary>
        readonly int[] pending = new int[10];
        /// <summary>Bets accepted by the server since the last ticket print.</summary>
        readonly int[] unprinted = new int[10];
        /// <summary>Text components that show the slip value for each number.</summary>
        readonly List<Text>[] numberTexts = new List<Text>[10];

        int serverBalance = 0;   // last balance confirmed by the server
        int serverPlay = 0;      // last play total confirmed by the server for this session
        bool betInFlight = false;
        int roundId = 0;         // bumped every time a round is reset, so late responses can be ignored

        static readonly string[] numberWords = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };

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
            // NumbersPositions = ResultView.resultView.NumberPositions;
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
            foreach (GameObject obj in allToDisable)
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

        bool WinHit = false;

        // Odds / Evens / Random only add chips to the slip (through GetNumbersBait); nothing is sent until Bet is pressed.
        public void SelectOdds()
        {
            if (!CanEditSlip)
            {
                return;
            }
            foreach (Button btn in odds)
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
            for (int i = 0; i < lim;)
            {
                int slectedRand = UnityEngine.Random.Range(0, allNumbersButton.Length);

                if (!usedNumbers.Contains(slectedRand))
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
            for (int i = 0; i < unprinted.Length; i++)
            {
                sb.Append("\n").Append(numberWords[i]).Append(" : ").Append(unprinted[i]);
                total += unprinted[i];
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

            if (GameController.has_network)
            {


                if (WinDeclared)
                {
                    WinDeclared = false;
                    SetPoisitionNow = false;
                    StopCoroutine(InstanceHolder);
                }
                //        Debug.Log(NumberPanel.GetComponent<Rigidbody2D>().angularVelocity);
                if (AllowRotation)
                {
                    SetPoisitionNow = false;
                    WinHit = false;
                    detect.StopDetecting = false;
                    time += Time.deltaTime;
                    //Debug.Log(time);
                    NumberPanel.transform.Rotate(new Vector3(0, 0, -1f) * _Speed * Time.deltaTime);

                    if (time >= 7.0f)
                    {
                        Debug.Log("Speed is: " + _Speed);
                        _Speed -= 1f;
                    }
                    else if (time >= 5.0f)
                    {
                        if (time == 5f)
                        {
                            // print("Calling win number");

                        }
                        _Speed -= 1f;
                        Debug.Log("Speed is: " + _Speed);
                    }


                    if (_Speed <= 100f)
                    {
                        Debug.Log("The Speed have been set to lowest");
                        AllowRotation = false;
                        SetPoisitionNow = true;
                        //_targetRotation = Quaternion.Euler(NumbersPositions[0]);
                        // NumberPanel.transform.rotation = _targetRotation;

                        endRotation = _targetRotation.eulerAngles.z;
                        startRotation = NumberPanel.transform.eulerAngles.z;
                        // InstanceHolder = StartCoroutine(GetWinNumber());
                    }

                }
                if (SetPoisitionNow)
                {
                    //detect.StopDetecting = true;
                    //endRotation += _SpeedTs * Time.deltaTime;
                    ////NumberPanel.transform.rotation = Quaternion.Euler(0f,0f, Mathf.Lerp(NumberPanel.transform.rotation.z, _targetRotation.z, _SpeedTs * Time.deltaTime));

                    //float eulerRot = Mathf.Lerp(startRotation, endRotation, .5f);
                    //NumberPanel.transform.eulerAngles = new Vector3(0, 0, eulerRot);
                    if (detect.LastObject.name != gotNumber && gotNumber != "")
                    {
                        // print("Not zero");
                        NumberPanel.transform.Rotate(new Vector3(0, 0, -1f) * _Speed * Time.deltaTime);
                    }

                    if (detect.LastObject.name == gotNumber && gotNumber != "")
                    {
                        _targetRotation = Quaternion.Euler(NumbersPositions[Convert.ToInt32(gotNumber)]);
                        NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, 1f * Time.deltaTime);

                        //if(NumberPanel.transform.rotation.z != _targetRotation.z)
                        //{
                        //    NumberPanel.transform.Rotate(new Vector3(0, 0, -1f) * _Speed * Time.deltaTime);
                        //}


                        if (!WinHit)
                        {

                            if (Xcrt != null)
                            {
                                StopCoroutine(Xcrt);
                                Xcrt = null;
                            }

                            if (GameController.selectedXVal == 0)
                            {
                                GameController.selectedXVal = 1;
                                //XNumber.text = "N";
                                xNum.sprite = xNumns[GameController.selectedXVal];
                            }
                            else
                            {
                                //XNumber.text = GameController.selectedXVal + "X";
                                xNum.sprite = xNumns[GameController.selectedXVal];
                            }

                            Xcrt = null;

                            WinHit = true;
                            InstanceHolder = StartCoroutine(GetWinNumber());
                        }

                    }


                    //if (detect.LastObject.name.Equals("0"))
                    //{
                    //    //Debug.Log("Rotating for zero");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[0]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //else if (detect.LastObject.name.Equals("1"))
                    //{
                    //    // Debug.Log("Rotating for one");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[1]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //else if (detect.LastObject.name.Equals("2"))
                    //{
                    //    // Debug.Log("Rotating for two");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[2]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //else if (detect.LastObject.name.Equals("3"))
                    //{
                    //    //Debug.Log("Rotating for thr");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[3]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //else if (detect.LastObject.name.Equals("4"))
                    //{
                    //    //  Debug.Log("Rotating for for");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[4]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //else if (detect.LastObject.name.Equals("5"))
                    //{
                    //    // Debug.Log("Rotating for fif");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[5]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //else if (detect.LastObject.name.Equals("6"))
                    //{
                    //    //  Debug.Log("Rotating for six");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[6]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //else if (detect.LastObject.name.Equals("7"))
                    //{
                    //    //                    Debug.Log("Rotating for sev");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[7]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //else if (detect.LastObject.name.Equals("8"))
                    //{
                    //    // Debug.Log("Rotating for eit");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[8]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //else if (detect.LastObject.name.Equals("9"))
                    //{
                    //    //Debug.Log("Rotating for nine");
                    //    _targetRotation = Quaternion.Euler(NumbersPositions[9]);
                    //    NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, _SpeedTs * Time.deltaTime);
                    //}
                    //NumberPanel.transform.Rotate(new Vector3(0,0,0));
                }
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
            if (multiplication == 9 && !Is_Spinning)
            {
                multiplication = 9;
                DoubleTextWin.SetActive(false);
                DoubleTextPlay.SetActive(false);
                popupmsg("Cancled");
            }

        }

        IEnumerator GetWinNumber()
        {
            yield return new WaitForSeconds(3f);

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
            if (numberFlashLight != null)
            {
                StopCoroutine(numberFlashLight);
            }

            numberFlashLight = StartCoroutine(ShowNumberWinReflect());
            WinDeclared = true;
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
                foreach (Transform trn in allLightHolder)
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

            if (status)
                switch (detect.LastObject.name)
                {


                    case "0":
                        MainPointsNow += NumberPoints.zero * multiplication;
                        tokensWon = NumberPoints.zero * multiplication;
                        won_on_number = 0;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                        break;
                    case "1":
                        MainPointsNow += NumberPoints.one * multiplication;
                        tokensWon = NumberPoints.one * multiplication;
                        won_on_number = 1;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                        break;
                    case "2":
                        MainPointsNow += NumberPoints.two * multiplication;
                        tokensWon = NumberPoints.two * multiplication;
                        won_on_number = 2;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                        break;
                    case "3":
                        MainPointsNow += NumberPoints.three * multiplication;
                        tokensWon = NumberPoints.three * multiplication;
                        won_on_number = 3;
                        // MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                        break;
                    case "4":
                        MainPointsNow += NumberPoints.four * multiplication;
                        tokensWon = NumberPoints.four * multiplication;
                        won_on_number = 4;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                        break;
                    case "5":
                        MainPointsNow += NumberPoints.five * multiplication;
                        tokensWon = NumberPoints.five * multiplication;
                        won_on_number = 5;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                        break;
                    case "6":
                        MainPointsNow += NumberPoints.six * multiplication;
                        tokensWon = NumberPoints.six * multiplication;
                        won_on_number = 6;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                        break;
                    case "7":
                        MainPointsNow += NumberPoints.seven * multiplication;
                        tokensWon = NumberPoints.seven * multiplication;
                        won_on_number = 7;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                        break;
                    case "8":
                        MainPointsNow += NumberPoints.eight * multiplication;
                        tokensWon = NumberPoints.eight * multiplication;
                        won_on_number = 8;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                        break;
                    case "9":
                        MainPointsNow += NumberPoints.nine * multiplication;
                        tokensWon = NumberPoints.nine * multiplication;
                        won_on_number = 9;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);

                        break;

                }
            else
            {
                //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                tokensWon = 0;
                won_on_number = -90;
            }

            multiplication = 9;
            //NAnim.enabled = true;
            NAnim.SetBool("Hit", true);
            NNumber.text = gotNumber.ToString();
            NNumber.gameObject.SetActive(true);
            SetServerBalance(MainPointsNow);

            if (status)
                Wintext.text = (Convert.ToInt32(Wintext.text) + tokensWon).ToString();

            GameObject obj = null;
            if (status)
            {
                //obj = BaitNumberPanelParent.transform.Find(detect.LastObject.name).gameObject;
                //obj.GetComponent<Animator>().SetBool("Play", true);

                popupmsg("+" + tokensWon.ToString());


            }
            DoubleTextWin.SetActive(false);
            DoubleTextPlay.SetActive(false);


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
            Clear(NumberDrawPanel, true);
            if (numberFlashLight != null)
            {
                StopCoroutine(numberFlashLight);
                numberFlashLight = null;
            }

            foreach (Transform trn in allLightHolder)
            {
                trn.gameObject.GetComponent<Image>().color = new Color(255f, 255f, 255f, 0f);
            }

            BlackLayover.SetActive(false);
            GameController.instance.CheckSession();

        }

        public void popupmsg(string text)
        {
            return;
            GameObject obj = Instantiate(front_msg_text);
            obj.GetComponent<RectTransform>().SetParent(fmsg_parent);
            obj.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            obj.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
            obj.GetComponent<TextMeshProUGUI>().text = text;
            //obj.GetComponent<TextMeshProUGUI>().fontSize = 100f;

            Destroy(obj, 3f);
        }

        public void SetChipValue(GameObject Obj)
        {
            if (_GlobalChipHolder != null && Obj == _GlobalChipHolder)
            {
                return;
            }
            ChipSelected = true;

            ChipValue = Obj.GetComponent<HoldingValues>().Value;
            Obj.transform.parent.GetComponent<Image>().enabled = true;
            if (_GlobalChipHolder != null)
            {
                _GlobalChipHolder.transform.parent.GetComponent<Image>().enabled = false;
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
            int[] totals = new int[10];
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
            RefreshBalanceTexts();
        }

        void RefreshBalanceTexts()
        {
            MainPoints.text = (serverBalance - PendingTotal).ToString();
            PlayPoints.text = (serverPlay + PendingTotal).ToString();
        }

        static bool TryGetNumber(GameObject go, out int n)
        {
            n = -1;
            return go != null && int.TryParse(go.name, out n) && n >= 0 && n <= 9;
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

        public void SpinWheel()
        {
            //baitList.Clear();
            if (!Is_Spinning && GameController.has_network)
            {
                // Safety net: anything still on the slip at spin time was never sent, so it isn't in play.
                DiscardPendingBets("Unplaced bets removed");
                //if (Convert.ToInt32(PlayPoints.text) > Convert.ToInt32(MainPoints.text))
                //{
                //    ScreenFlowHandler.screenFlow.ShowErr();
                //    return;
                //}
                Is_Spinning = true;
                //_Speed = (float)UnityEngine.Random.Range(20, 30);
                _Speed = 500f;
                time = 0f;
                AllowRotation = true;
                Xcrt = StartCoroutine(ShowXVal());
                //SpinWheelAudio.Play();

            }


        }

        public IEnumerator ShowXVal()
        {
            float waitTime = .05f;
            int val = 1;
            int counter = 1;
            xNum.gameObject.SetActive(true);
            while (Is_Spinning)
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

                if (counter == 5)
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
            DoubleTextWin.SetActive(false);
            DoubleTextPlay.SetActive(false);

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
            DoubleTextWin.SetActive(false);
            DoubleTextPlay.SetActive(false);
        }

        /// <summary>Repeat: puts last round's bets on the slip (replaces the current slip, so double-clicking doesn't double it).</summary>
        public void Repeat()
        {
            if (!CanEditSlip)
            {
                return;
            }

            int[] last = new int[10];
            int lastTotal = 0;
            foreach (KeyValuePair<string, int> numVal in numberValues)
            {
                int n;
                if (int.TryParse(numVal.Key, out n) && n >= 0 && n <= 9 && numVal.Value > 0)
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