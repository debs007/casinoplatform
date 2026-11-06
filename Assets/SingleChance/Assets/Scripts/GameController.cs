using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using UnityEngine.Networking;
using System;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace SingleChance
{
    public class GameController : MonoBehaviour
    {
        public Text HourText, MinText;
        int InitialTime = 165, tempTime = 0;
        public static bool TimerStarted = false;
        Coroutine TimerRoutine;
        public Button SpinButton;
        public GameObject DevPanel;
        bool NotConnected = false;
        public static bool has_network = true;
        //[DllImport("__Internal")]
        //private static extern int doesConnectionExist();
        // Start is called before the first frame update
        public static GameController instance;
        public string BaseUri;

        public int percentage;
        public static int selectedXVal = 1;
        Camera m_camera;
        public AudioSource clickAudio;
        [SerializeField] GraphicRaycaster m_Raycaster;
        PointerEventData m_PointerEventData;
        [SerializeField] EventSystem m_EventSystem;
        [SerializeField] RectTransform canvasRect;

        public GameObject Notification;
        public Text notiText;

        public AudioSource NotificationSource;

        public AudioClip placeyourchip, lastchance, nomorebet;
        public GameObject SessionObject;

        public GameObject rows;
        public RectTransform table;
        public InputField namefield, password;
        public Text DateResultscreen, totalPlayPoint, totalWinPoint;
        public InputField oldPass, newPass;
        public Text ShowWrong;
        public Button clrBtn;
        public Image TimeImage;
        public GameObject GameInProgress;
        public Button doubleBtn, repeatBtn, clearBtn;

        public Text[] allBaitValuesTexts;

        public Text sessionNum;
        int backupTime;

        void Start()
        {
            instance = this;
            if (PlayerPrefs.HasKey("name"))
            {
                namefield.text = PlayerPrefs.GetString("name");
                password.text = PlayerPrefs.GetString("pass");
            }
            //StartCoroutine(CheckNetwork());
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            m_camera = Camera.main;
            GameInProgress.SetActive(true);
            Application.targetFrameRate = 60;
            StartCoroutine(HideZeros());
            //GetPoints.getPoints.DisableAll();
            backupTime = 40;
        }

        public void ChangePassword()
        {
            if (string.IsNullOrEmpty(oldPass.text.Trim(' ')) || string.IsNullOrEmpty(newPass.text.Trim(' ')))
            {
                ShowWrong.text = "Enter all field's value";
                ShowWrong.color = Color.red;
                return;
            }

            StartCoroutine(UpdatePassword());
        }

        IEnumerator UpdatePassword()
        {
            WWWForm form = new WWWForm();
            form.AddField("old", oldPass.text);
            form.AddField("new", newPass.text);
            form.AddField("id", Constants.userData.id);

            UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriApi + "changePassword", form);
            request.timeout = 20;
            ShowWrong.text = "Updating Password";
            ShowWrong.color = Color.white;
            yield return request.SendWebRequest();
            Debug.LogError(request.downloadHandler.text);

            if (request.downloadHandler.text == "done")
            {
                ShowWrong.text = "Password reset successfully!";
                ShowWrong.color = Color.green;
            }
            else
            {
                ShowWrong.text = "Password reset failed!";
                ShowWrong.color = Color.red;
            }
        }

        public void ShowNotification(string msg, AudioClip clip)
        {
            NotificationSource.clip = clip;
            notiText.text = msg;
            NotificationSource.Play();
            Notification.SetActive(true);

            CancelInvoke("DeactivateNotification"); // an older pending hide would cut this message short
            Invoke("DeactivateNotification", 3f);
        }

        /// <summary>Same banner as ShowNotification, without a sound (used for bet placed / failed messages).</summary>
        public void ShowMessage(string msg)
        {
            notiText.text = msg;
            Notification.SetActive(true);
            CancelInvoke("DeactivateNotification");
            Invoke("DeactivateNotification", 3f);
        }

        void DeactivateNotification()
        {
            Notification.SetActive(false);
        }

        //IEnumerator CheckNetwork()
        //{
        //    while (true)
        //    {
        //        int conn = -2;
        //        yield return conn = doesConnectionExist();
        //        if (conn == 1)
        //        {
        //        ScreenFlowHandler.screenFlow.OpenPopup("NoInternet");

        //        has_network = false;
        //        // Debug.Log("Not coneected");
        //        }
        //        else if(conn == 0)
        //        {
        //            Time.timeScale = 1;
        //            ScreenFlowHandler.screenFlow.closePopup();
        //            has_network = true;
        //            // Debug.Log("connected");
        //        }

        //    }

        //}
        DateTime lastMinimized;

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                lastMinimized = DateTime.Now;
                if (sliderRoutine != null)
                {
                    StopCoroutine(sliderRoutine);
                    sliderRoutine = null;
                }
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            try
            {
                if (lastMinimized != null)
                {
                    if (focus)
                    {
                        int totalSec = Convert.ToInt32((DateTime.Now - lastMinimized).TotalSeconds);
                        backupTime -= totalSec;
                        sec = ++backupTime;
                        if (backupTime <= 10)
                        {
                            ScreenFlowHandler.instance.ReloadScene();
                        }
                        else
                        {
                            StartTimer(sec);
                            sliderRoutine = StartCoroutine(SlideDownTime(backupTime));
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
        }


        IEnumerator HideZeros()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                foreach (Text txt in allBaitValuesTexts)
                {
                    if (txt.text == "0")
                    {
                        txt.color = new Color(0f, 0f, 0f, 0f);
                    }
                    else
                    {
                        txt.color = new Color(0f, 0f, 0f, 1f);
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {


            // Double / Clear work on the unsent slip only
            bool slipEditable = GetPoints.getPoints != null && GetPoints.getPoints.HasPendingBets && GetPoints.getPoints.CanEditSlip;
            doubleBtn.interactable = slipEditable;
            clearBtn.interactable = slipEditable;


            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
            {
                Mouse mouse = Mouse.current;
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    //print("hit");
                    m_PointerEventData = new PointerEventData(m_EventSystem);
                    //Set the Pointer Event Position to that of the game object
                    m_PointerEventData.position = Input.mousePosition;

                    //Create a list of Raycast Results
                    List<RaycastResult> results = new List<RaycastResult>();

                    //Raycast using the Graphics Raycaster and mouse click position
                    m_Raycaster.Raycast(m_PointerEventData, results);


                    foreach (RaycastResult res in results)
                    {

                        // print(res.gameObject.name);
                        if (res.gameObject.GetComponent<Button>() != null)
                        {
                            print("Found sound");
                            if (GetPoints.allowBait)
                                clickAudio.Play();
                        }
                    }
                }
            }




            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    m_PointerEventData = new PointerEventData(m_EventSystem);
                    //Set the Pointer Event Position to that of the game object
                    m_PointerEventData.position = touch.position;

                    //Create a list of Raycast Results
                    List<RaycastResult> results = new List<RaycastResult>();

                    //Raycast using the Graphics Raycaster and mouse click position
                    m_Raycaster.Raycast(m_PointerEventData, results);


                    foreach (RaycastResult res in results)
                    {

                        // print(res.gameObject.name);
                        if (res.gameObject.GetComponent<Button>() != null)
                        {
                            print("Found sound mobile");
                            if (GetPoints.allowBait)
                                clickAudio.Play();
                        }
                    }
                }

            }
        }


        public void CheckSession()
        {
            GetPoints.Is_Spinning = false;
            StartCoroutine(GetSession());

        }

        Coroutine sliderRoutine;

        public IEnumerator GetSession()
        {
            UnityWebRequest request = UnityWebRequest.Get(Constants.baseUriSession);
            request.timeout = 100;
            SessionObject.SetActive(true);
            yield return request.SendWebRequest();

            Debug.LogError("Session " + request.downloadHandler.text);

            Constants.sessionData = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);

            if (Constants.sessionData.diff > 12 && Constants.sessionData.diff <= Constants.sessionData.totalTime)
            {
                TimerStarted = true;
                GameInProgress.SetActive(false);
                SessionObject.SetActive(false);
                StartTimer(Constants.sessionData.diff);
                sliderRoutine = StartCoroutine(SlideDownTime(Constants.sessionData.diff));
                ResultView.resultView.populateResult(Constants.sessionData.bets);
                GetPoints.getPoints.DisableAll();
                GetPoints.getPoints.NNumber.text = "";
                sessionNum.text = Constants.sessionData.sessionNum;
                CheckRepeat();
                GetPoints.gotNumber = "";
            }
            else
            {
                Invoke("CheckSession", 2f);
            }
        }

        public void CheckRepeat()
        {
            if (GetPoints.getPoints.numberValues.Count > 0)
            {
                if (GetPoints.allowBait)
                    repeatBtn.interactable = true;
            }
            else
            {
                repeatBtn.interactable = false;
            }
        }

        public void MyBets()
        {

            foreach (Transform trn in table)
            {
                Destroy(trn.gameObject);
            }

            StartCoroutine(GetMyBets());
        }

        public IEnumerator SlideDownTime(int time)
        {
            TimeImage.fillAmount = 0f;
            while (TimeImage.fillAmount < 1f)
            {
                yield return new WaitForSeconds(time / 100f);
                TimeImage.fillAmount += 0.0106f;
            }
        }

        public IEnumerator GetMyBets()
        {

            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);

            UnityWebRequest request = UnityWebRequest.Post(Constants.getmyBets, form);

            request.timeout = 100;
            yield return request.SendWebRequest();
            Debug.LogError(request.downloadHandler.text);
            MyBets myBets = JsonUtility.FromJson<MyBets>(request.downloadHandler.text);
            //        DateResultscreen.text = myBets.date;
            int pp = 0; int wp = 0;

            int i = 0;
            foreach (MyBetData data in myBets.allBets)
            {
                //i++;
                GameObject obj = Instantiate(rows);
                obj.transform.GetChild(1).gameObject.GetComponent<Text>().text = (++i).ToString();
                obj.transform.GetChild(2).gameObject.GetComponent<Text>().text = data.sessionId.Substring(10);
                obj.transform.GetChild(3).gameObject.GetComponent<Text>().text = data.amount;
                obj.transform.GetChild(4).gameObject.GetComponent<Text>().text = data.win;

                Debug.LogError("pp " + data.amount + " wp " + data.win);

                if (!string.IsNullOrEmpty(data.amount) && data.amount != "N/A")
                {
                    pp += Convert.ToInt32(data.amount);
                }
                if (!string.IsNullOrEmpty(data.win) && data.win != "N/A")
                {
                    wp += Convert.ToInt32(data.win);
                }



                //obj.transform.GetChild(5).gameObject.GetComponent<Text>().text = data.won;

                //string[] bettings = data.bets.Split('|');
                //foreach(Transform obs in obj.transform.GetChild(6))
                //{
                //    obs.GetChild(0).gameObject.GetComponent<Text>().text = bettings[i];
                //    i++;
                //}

                obj.GetComponent<RectTransform>().SetParent(table);
                obj.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
            }

            totalPlayPoint.text = pp.ToString();
            totalWinPoint.text = wp.ToString();
        }

        static readonly string[] betFieldNames = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };

        /// <summary>
        /// Sends the whole bet slip in one updateBet call (same form fields as before: session totals per number).
        /// onDone(ok, balance, play, error) — balance/play are the server's "balance|play" reply.
        /// </summary>
        public IEnumerator SubmitBets(string session, int[] totals, Action<bool, int, int, string> onDone)
        {
            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);
            form.AddField("session", session);
            int totalAmt = 0;
            for (int i = 0; i < totals.Length; i++)
            {
                totalAmt += totals[i];
            }
            form.AddField("amount", totalAmt);
            for (int i = 0; i < betFieldNames.Length; i++)
            {
                form.AddField(betFieldNames[i], totals[i]);
            }

            using (UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriUpdate, form))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();

                string body = request.downloadHandler != null ? request.downloadHandler.text : "";
                Debug.Log("updateBet [" + request.responseCode + "] " + body);

                int balance = 0, play = 0;
                bool ok = request.result == UnityWebRequest.Result.Success
                          && request.responseCode == 200
                          && TryParseBetReply(body, out balance, out play);

                string error = null;
                if (!ok)
                {
                    error = !string.IsNullOrEmpty(request.error) ? request.error : ("Unexpected reply: " + body);
                }
                onDone(ok, balance, play, error);
            }
        }

        /// <summary>updateBet replies "balance|play". Anything else is treated as a failure instead of being written to the UI.</summary>
        static bool TryParseBetReply(string body, out int balance, out int play)
        {
            balance = 0;
            play = 0;
            if (string.IsNullOrEmpty(body))
            {
                return false;
            }
            string[] parts = body.Trim().Split('|');
            return parts.Length >= 2
                   && int.TryParse(parts[0].Trim(), out balance)
                   && int.TryParse(parts[1].Trim(), out play);
        }

        void StartTimer(int TimeToStart)
        {
            if (TimerRoutine != null)
            {
                StopCoroutine(TimerRoutine);
            }
            TimerRoutine = StartCoroutine(StartClock(TimeToStart));
        }
        int sec = 0;
        IEnumerator StartClock(int TimeToStart)
        {
            ShowNotification("Place Your Chip", placeyourchip);



            int min = ((int)TimeToStart / 60);
            HourText.text = "0" + min.ToString();
            sec = TimeToStart - (min * 60);
            MinText.text = sec.ToString();
            while (true)
            {
                if (GameController.has_network)
                {
                    while (sec > 0)
                    {

                        sec--;
                        TimeToStart--;
                        backupTime = TimeToStart;

                        if (TimeToStart == Constants.sessionData.lastChance)
                        {
                            ShowNotification("Last Chance", lastchance);
                        }

                        if (TimeToStart <= Constants.sessionData.minLimit)
                        {
                            GetPoints.allowBait = false;
                            // Chips left on the slip without pressing Bet are not placed (no-op after the first tick)
                            GetPoints.getPoints.DiscardPendingBets("Time over - unplaced bets removed");

                            GetPoints.getPoints.Wintext.text = "0";
                            if (sec == 5)
                            {
                                GetPoints.getPoints.NAnim.SetBool("Hit", false);
                                GetPoints.getPoints.XNumber.text = "";

                                GetPoints.getPoints.NNumber.gameObject.SetActive(false);
                                ShowNotification("No More Bet", nomorebet);
                                SetRepeatValues();
                                GetPoints.getPoints.DisableAll();
                                // StartCoroutine(UpdateBetReport());
                                //StartCoroutine(GetLastNumber());
                            }

                        }

                        if (sec >= 10)
                        {
                            MinText.text = sec.ToString();
                        }
                        else
                        {
                            MinText.text = "0" + sec.ToString();
                        }
                        yield return new WaitForSeconds(1f);
                    }
                    min--;

                    if (min >= 0)
                    {
                        HourText.text = "0" + min.ToString();
                        sec = 60;
                    }
                    else
                        break;
                }

            }

            GetPoints.getPoints.SpinWheel();
            Invoke("GetWinNum", 1f);
        }

        public void SetRepeatValues()
        {

            if (NumberPoints.Total > 0)
            {
                GetPoints.getPoints.numberValues.Clear();
                // only what the server accepted (PlayPoints text can include an unsent slip)
                GetPoints.getPoints.LastBait = NumberPoints.Total;
                GetPoints.getPoints.numberValues.Add("0", NumberPoints.zero);
                GetPoints.getPoints.numberValues.Add("1", NumberPoints.one);
                GetPoints.getPoints.numberValues.Add("2", NumberPoints.two);
                GetPoints.getPoints.numberValues.Add("3", NumberPoints.three);
                GetPoints.getPoints.numberValues.Add("4", NumberPoints.four);
                GetPoints.getPoints.numberValues.Add("5", NumberPoints.five);
                GetPoints.getPoints.numberValues.Add("6", NumberPoints.six);
                GetPoints.getPoints.numberValues.Add("7", NumberPoints.seven);
                GetPoints.getPoints.numberValues.Add("8", NumberPoints.eight);
                GetPoints.getPoints.numberValues.Add("9", NumberPoints.nine);

                // copy, not alias: baitList is cleared at round end and used to wipe this too
                GetPoints.getPoints.repeat = new List<int>(GetPoints.getPoints.baitList);


            }


        }


        public void GetWinNum()
        {
            StartCoroutine(GetWinNumber());
        }

        IEnumerator GetWinNumber()
        {
            WWWForm form = new WWWForm();
            form.AddField("session", Constants.sessionData.session);
            Debug.LogError(">>>firing " + Constants.sessionData.session);
            UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriWinnumber, form);
            request.timeout = 100;
            yield return request.SendWebRequest();
            Debug.LogError(">>> " + request.downloadHandler.text);

            if (request.responseCode == 200)
            {
                GetWinNumber number = JsonUtility.FromJson<GetWinNumber>(request.downloadHandler.text);

                GetPoints.gotNumber = number.number;
                selectedXVal = number.times;
            }
            else
            {
                GetWinNum();
            }

        }

        public IEnumerator Setwin()
        {
            WWWForm form = new WWWForm();
            form.AddField("session", Constants.sessionData.session);
            form.AddField("id", Constants.userData.id);

            UnityWebRequest request = UnityWebRequest.Post(Constants.setWin, form);
            request.timeout = 100;
            yield return request.SendWebRequest();
            Debug.LogError("Win report " + request.downloadHandler.text);

            int newBalance;
            if (request.responseCode == 200 && int.TryParse(request.downloadHandler.text.Trim(), out newBalance))
            {
                GetPoints.getPoints.SetServerBalance(newBalance);
            }

            //GetPoints.getPoints.Wintext.text = request.downloadHandler.text;
        }

        IEnumerator GetLastNumber()
        {


            yield return null;


            List<int> lowest = new List<int>();
            List<int> backup = new List<int>();
            lowest.Add(NumberPoints.zero);
            lowest.Add(NumberPoints.one);
            lowest.Add(NumberPoints.two);
            lowest.Add(NumberPoints.three);
            lowest.Add(NumberPoints.four);
            lowest.Add(NumberPoints.five);
            lowest.Add(NumberPoints.six);
            lowest.Add(NumberPoints.seven);
            lowest.Add(NumberPoints.eight);
            lowest.Add(NumberPoints.nine);



            int min;
            List<int> mins = new List<int>();




            int[] allmins = lowest.ToArray();
            Array.Sort(allmins);

            int gotRand = UnityEngine.Random.Range(0, 4);
            int gotNumber = allmins[gotRand];

            if (NumberPoints.zero == gotNumber)
            {
                mins.Add(0);
            }
            if (NumberPoints.one == gotNumber)
            {
                mins.Add(1);
            }
            if (NumberPoints.two == gotNumber)
            {
                mins.Add(2);
            }
            if (NumberPoints.three == gotNumber)
            {
                mins.Add(3);
            }
            if (NumberPoints.four == gotNumber)
            {
                mins.Add(4);
            }
            if (NumberPoints.five == gotNumber)
            {
                mins.Add(5);
            }
            if (NumberPoints.six == gotNumber)
            {
                mins.Add(6);
            }
            if (NumberPoints.seven == gotNumber)
            {
                mins.Add(7);
            }
            if (NumberPoints.eight == gotNumber)
            {
                mins.Add(8);
            }
            if (NumberPoints.nine == gotNumber)
            {
                mins.Add(9);
            }

            GetPoints.gotNumber = mins[UnityEngine.Random.Range(0, mins.Count)].ToString();
            //GetXVal(gotNumber);
        }

        public void GetXVal(int gotNumber)
        {
            int PlayPointVal = Convert.ToInt32(GetPoints.getPoints.PlayPoints.text);

            if (true)
            {
                // Debug.LogError("Number x selected " + gotNumber +" with value "+ gotNumber * 9 * 5);

                if (gotNumber * 9 * 5 <= (PlayPointVal * percentage / 100))
                {
                    selectedXVal = 5;
                    print("5-");
                }
                else if (gotNumber * 9 * 4 <= (PlayPointVal * percentage / 100))
                {
                    selectedXVal = 4;
                    print("4-");
                }
                else if (gotNumber * 9 * 3 <= (PlayPointVal * percentage / 100))
                {
                    selectedXVal = 3;
                    print("3-");
                }
                else if (gotNumber * 9 * 2 <= (PlayPointVal * percentage / 100))
                {
                    selectedXVal = 2;
                    print("2-");
                }
                else
                {
                    selectedXVal = 1;
                }
            }


        }

        public void ClosePanel(GameObject obj)
        {
            obj.SetActive(false);
            if (obj.name.Equals("DevPanel"))
            {

            }
        }
    }
}
