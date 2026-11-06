using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using UnityEngine.Networking;
using System;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using ceTe.DynamicPDF.Printing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.IO;

namespace TwelveCards
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
        public static int WinAmountGot = 0;
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

        public TextMeshProUGUI drawtime;
        public Text Commision, Ntp;
        public string DrawTime;
        public Text TicketId;
        public TMP_InputField From, To;
        //public Image filler;

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
            selectedXVal = 1;
            WinAmountGot = 0;
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

            oldPass.text = "";
            newPass.text = "";
        }

        public void ShowNotification(string msg, AudioClip clip)
        {
            NotificationSource.clip = clip;
            notiText.text = msg;
            NotificationSource.Play();
            Notification.SetActive(true);

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
            //yield break;
            while (true)
            {
                yield return new WaitForEndOfFrame();
                foreach (Text txt in allBaitValuesTexts)
                {
                    if (txt.text == "0")
                    {
                        txt.color = new Color(0f, 0f, 0f, 0f);
                        txt.gameObject.transform.GetChild(0).gameObject.SetActive(true);
                    }
                    else
                    {
                        txt.color = new Color(0f, 0f, 0f, 1f);
                        txt.gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {


            if (NumberPoints.zero > 0 || NumberPoints.one > 0 || NumberPoints.two > 0 || NumberPoints.three > 0 || NumberPoints.four > 0 || NumberPoints.five > 0 || NumberPoints.six > 0 || NumberPoints.seven > 0 || NumberPoints.eight > 0 || NumberPoints.nine > 0 || NumberPoints.ten > 0 || NumberPoints.eleven > 0)
            {
                if (GetPoints.allowBait)
                {
                    doubleBtn.interactable = true;
                    clearBtn.interactable = true;
                }

            }
            else
            {
                doubleBtn.interactable = false;
                clearBtn.interactable = false;
            }


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
            // request.certificateHandler = new BypassCertificate();
            request.timeout = 100;
            SessionObject.SetActive(true);
            //request.certificateHandler = new BypassCertificate();
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
                if (Constants.sessionData.bets != null)
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
            TicketId.text = "Ticket Id";
            //StartCoroutine(GetMyBets());
            ShowTickets();
        }

        public void AllBets()
        {

            foreach (Transform trn in table)
            {
                Destroy(trn.gameObject);
            }
            TicketId.text = "Session No";
            StartCoroutine(GetMyBets("mine"));
        }

        public void ViewDateWise()
        {
            if (string.IsNullOrEmpty(From.text.Trim()) || string.IsNullOrEmpty(To.text.Trim()))
            {
                return;
            }

            foreach (Transform trn in table)
            {
                Destroy(trn.gameObject);
            }
            TicketId.text = "Session No";
            StartCoroutine(GetMyBets("mine", useTime: true));
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

        public void PrintReport()
        {
            PdfAlignmentStyle style = PdfAlignmentStyle.TopCenter;
            Syncfusion.Pdf.PdfDocument document = new Syncfusion.Pdf.PdfDocument();
            float pageY = 230f;

            //Add a page to the document.

            PdfPage page = document.Pages.Add();
            page.Section.PageSettings.SetMargins(0f);
            page.Section.PageSettings.Size = new Syncfusion.Drawing.SizeF(175f, pageY);

            //Create PDF graphics for the page.

            PdfGraphics graphics = page.Graphics;


            //Set the standard font.

            PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 8);
            PdfFont fontGen = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
            PdfFont fontLogo = new PdfStandardFont(PdfFontFamily.Helvetica, 17);
            float yVal = 44f;
            float xVal = 22f;
            float yIncr = 12f;
            float xIncr = 30f;
            //Draw the text.
            graphics.DrawString("For Ammusement Only", font, PdfBrushes.Black, new Syncfusion.Drawing.PointF(55f, 8f));
            graphics.DrawString("KHELO JEETO", fontLogo, PdfBrushes.Black, new Syncfusion.Drawing.PointF(35f, 15f));
            graphics.DrawString("RETAILER SALES REPORT", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Retailer Name : " + Constants.userData.name, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("From Date : " + From.text, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("To Date : " + To.text, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Sale Point : " + totalPlayPoint.text, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Win Point : " + totalWinPoint.text, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Calim Point : " + totalWinPoint.text, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Commi Point : " + Commision.text, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("NTP Point : " + Ntp.text, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));

            MemoryStream stream = new MemoryStream();

            //Save the document into memory stream.

            document.Save(stream);

            //If the position is not set to '0' then the PDF will be empty.

            stream.Position = 0;

            //Close the document.

            //Texture2D tex = new Texture2D(100, 100);
            //tex.LoadImage(stream.ToArray());
            //byte[] afterEffect = tex.EncodeToPNG();
            //File.WriteAllBytes(Path.Combine(Application.streamingAssetsPath, "jeetojoker.png"), afterEffect);
            string rand = UnityEngine.Random.Range(100000, 999999).ToString();
            string path = Path.Combine(Application.persistentDataPath, "JNS.pdf");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.WriteAllBytes(path, stream.ToArray());


            //var printDoc = new PrintDocument();


            try
            {


                if (true)
                {
                    // LicensingException licensing = new LicensingException("");
                    //System.Drawing.Printing.PrintDocument document1 = new System.Drawing.Printing.PrintDocument();

                    //document1.Print();
                    //PdfPrintSettings.

                    PrintJob job = new PrintJob(Printer.Default, path);
                    //job.PrintOptions.SetPaperSizeByName()
                    //System.Drawing.Printing.PaperSize paperSize = new System.Drawing.Printing.PaperSize("", 100, 99);
                    job.PrintOptions.HorizontalAlign = HorizontalAlign.Center;
                    job.PrintOptions.VerticalAlign = VerticalAlign.Top;
                    job.Print();

                    //PrintJob.AddLicense()



                    //using (var doc = PdfiumViewer.PdfDocument.Load(path))
                    //{
                    //    using(var printDocument = doc.CreatePrintDocument())
                    //    {
                    //        printDocument.Print();
                    //    }
                    //}

                    // Don't set the PrinterName; it will use the system's default printer.




                }


            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.StackTrace);
            }
        }

        public void ShowTickets()
        {
            int pp = 0; int wp = 0;

            int i = 0;

            if (PlayerPrefs.HasKey("tickets"))
            {
                string gotData = PlayerPrefs.GetString("tickets");
                Stores stores = JsonUtility.FromJson<Stores>(gotData);

                Constants.tickets = stores.mybets;
                //stores.mybets.Reverse();
                List<MyBetData> allCopy = new List<MyBetData>();

                foreach (MyBetData data in stores.mybets)
                {
                    allCopy.Add(data);
                }
                allCopy.Reverse();
                foreach (MyBetData data in allCopy)
                {
                    //i++;
                    GameObject obj = Instantiate(rows);
                    obj.transform.GetChild(0).gameObject.GetComponent<Text>().text = data.sessionNum.ToString();
                    obj.transform.GetChild(1).gameObject.GetComponent<Text>().text = data.sessionId.Substring(10);
                    string[] allBets = data.bets.Split('|');
                    string projection = "";
                    int counter = 0;
                    foreach (string crd in allBets)
                    {
                        if (crd != "0")
                        {
                            projection += Constants.cardsWithSymbol[counter] + " ";
                        }

                        counter++;
                    }

                    obj.transform.GetChild(2).gameObject.GetComponent<Text>().text = projection;
                    obj.transform.GetChild(3).gameObject.GetComponent<Text>().text = data.amount;
                    obj.transform.GetChild(4).gameObject.GetComponent<Text>().text = data.win;
                    obj.transform.GetChild(5).gameObject.GetComponent<Text>().text = data.time;
                    if (data.number != -1)
                        obj.transform.GetChild(6).gameObject.GetComponent<Text>().text = Constants.cardsWithSymbol[data.number];
                    else
                        obj.transform.GetChild(6).gameObject.GetComponent<Text>().text = "--";

                    obj.transform.GetChild(7).gameObject.SetActive(true);
                    obj.transform.GetChild(8).gameObject.SetActive(true);

                    obj.transform.GetChild(7).gameObject.GetComponent<Text>().text = $"BB: {data.beforeBalance}";
                    obj.transform.GetChild(8).gameObject.GetComponent<Text>().text = $"BA: {data.afterBalance}";



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
            }
        }

        public IEnumerator GetMyBets(string type = "mine", bool useTime = false)
        {

            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);

            if (useTime)
            {
                string from = From.text.Split('-')[2] + "-" + From.text.Split('-')[1] + "-" + From.text.Split('-')[0] + " 00:00:00";
                string to = To.text.Split('-')[2] + "-" + To.text.Split('-')[1] + "-" + To.text.Split('-')[0] + " 00:00:00";

                Debug.LogError(from + ":" + to);

                form.AddField("from", from);
                form.AddField("to", to);
            }
            form.AddField("type", type);


            UnityWebRequest request = UnityWebRequest.Post(Constants.getmyBets, form);
            // request.certificateHandler = new BypassCertificate();

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
                obj.transform.GetChild(0).gameObject.GetComponent<Text>().text = data.sessionNum.ToString();
                obj.transform.GetChild(1).gameObject.GetComponent<Text>().text = data.sessionId.Substring(10);
                string[] allBets = data.bets.Split('|');
                string projection = "";
                int counter = 0;
                foreach (string crd in allBets)
                {
                    if (crd != "0")
                    {
                        projection += Constants.cardsWithSymbol[counter] + " ";
                    }

                    counter++;
                }

                obj.transform.GetChild(2).gameObject.GetComponent<Text>().text = projection;
                obj.transform.GetChild(3).gameObject.GetComponent<Text>().text = data.amount;
                obj.transform.GetChild(4).gameObject.GetComponent<Text>().text = data.win;
                obj.transform.GetChild(5).gameObject.GetComponent<Text>().text = data.time;
                if (data.number != -1)
                    obj.transform.GetChild(6).gameObject.GetComponent<Text>().text = Constants.cardsWithSymbol[data.number];
                else
                    obj.transform.GetChild(6).gameObject.GetComponent<Text>().text = "--";

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

            totalPlayPoint.text = "Loading..";
            totalWinPoint.text = "Loading..";
            int ntp = (pp - wp) - Constants.userData.commisionReceived;
            Commision.text = "Loading..";
            //if(ntp < 0)
            //{
            //    ntp = 0;
            //}
            Ntp.text = "Loading..";
        }

        public void GetCurrentReport()
        {
            StartCoroutine(LoginDirectly());
        }

        IEnumerator LoginDirectly()
        {
            WWWForm form = new();
            form.AddField("userName", PlayerPrefs.GetString("name"));
            form.AddField("password", PlayerPrefs.GetString("pass"));
            UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriDeatils, form);



            yield return request.SendWebRequest();


            bool login = false;
            Debug.LogError(request.downloadHandler.text);


            if (request.responseCode == 200)
            {

                UserResponse userResponse = JsonUtility.FromJson<UserResponse>(request.downloadHandler.text);
                totalPlayPoint.text = userResponse.data.totalPlayPoints;
                totalWinPoint.text = userResponse.data.winPoint;
                int pp = Convert.ToInt32(userResponse.data.totalPlayPoints);
                int wp = Convert.ToInt32(userResponse.data.winPoint);
                int ntp = (pp - wp) - Constants.userData.commisionReceived;
                Commision.text = Constants.userData.commisionReceived.ToString();

                Ntp.text = ntp.ToString();

            }

        }

        public IEnumerator UpdateBetReport(int onNumber = -1, int chipValue = 0)
        {
            if (true)
            {
                WWWForm form = new WWWForm();
                form.AddField("id", Constants.userData.id);
                form.AddField("session", Constants.sessionData.session);
                int totalAmt = NumberPoints.zero + NumberPoints.one + NumberPoints.two + NumberPoints.three + NumberPoints.four + NumberPoints.five + NumberPoints.six + NumberPoints.seven + NumberPoints.eight + NumberPoints.nine + NumberPoints.ten + NumberPoints.eleven;
                form.AddField("amount", totalAmt);
                Debug.LogError("Fired with" + onNumber);

                form.AddField("zero", NumberPoints.zero);
                form.AddField("one", NumberPoints.one);
                form.AddField("two", NumberPoints.two);
                form.AddField("three", NumberPoints.three);
                form.AddField("four", NumberPoints.four);
                form.AddField("five", NumberPoints.five);
                form.AddField("six", NumberPoints.six);
                form.AddField("seven", NumberPoints.seven);
                form.AddField("eight", NumberPoints.eight);
                form.AddField("nine", NumberPoints.nine);
                form.AddField("ten", NumberPoints.ten);
                form.AddField("eleven", NumberPoints.eleven);


                UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriUpdate, form);
                //  request.certificateHandler = new BypassCertificate();
                request.timeout = 10;
                Debug.LogError("Updating bet");
                yield return request.SendWebRequest();
                Debug.LogError("Updating bet " + request.downloadHandler.text);
                if (request.responseCode == 200)
                {
                    GetPoints.getPoints.MainPoints.text = request.downloadHandler.text.Split('|')[0];
                    GetPoints.getPoints.PlayPoints.text = request.downloadHandler.text.Split('|')[1];
                    GetPoints.getPoints.PrintAction();
                }
                else
                {

                    // GetPoints.getPoints.Clear(GetPoints.getPoints.NumberDrawPanel, false, SpecialAccess: true);
                    GetPoints.getPoints.CLR(GetPoints.getPoints.NumberDrawPanel);
                }
            }
            else
            {
                //yield return null;
            }


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

            DateTime currentTime = DateTime.Now;
            currentTime = currentTime.AddSeconds(TimeToStart);
            string timeNow = currentTime.ToString();
            DrawTime = timeNow;
            string[] allTimes = timeNow.Split(' ');

            drawtime.text = allTimes[1];
            sec = TimeToStart;
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

                            GetPoints.getPoints.Wintext.text = "0";
                            if (sec == 5)
                            {
                                GetPoints.getPoints.NAnim.SetBool("Hit", false);
                                GetPoints.getPoints.XNumber.text = "";

                                GetPoints.getPoints.NNumber.gameObject.SetActive(false);
                                ShowNotification("No More Bet", nomorebet);
                                //GetPoints.getPoints.TestButton();
                                //SetRepeatValues();
                                GameObject.FindGameObjectWithTag("clearBtn").GetComponent<Button>().onClick.Invoke();
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
                    //min--;

                    //if (min >= 0)
                    //{
                    //    HourText.text = "0" + min.ToString();
                    //   // sec = 60;
                    //}
                    //else
                    //    break;

                    break;
                }

            }

            GetPoints.getPoints.SpinWheel();
            Invoke("GetWinNum", 1f);
        }

        public void UpdateBetInstant(int onNumber, int chipValue, bool FromBet = false)
        {
            if (!FromBet)
                return;

            StartCoroutine(UpdateBetReport(onNumber, chipValue));
        }

        public void SetRepeatValues()
        {

            if (NumberPoints.zero != 0 || NumberPoints.one != 0 || NumberPoints.two != 0 || NumberPoints.three != 0 || NumberPoints.four != 0 || NumberPoints.five != 0 || NumberPoints.six != 0 || NumberPoints.seven != 0 || NumberPoints.eight != 0 || NumberPoints.nine != 0 || NumberPoints.ten != 0 || NumberPoints.eleven != 0)
            {
                GetPoints.getPoints.numberValues.Clear();
                GetPoints.getPoints.LastBait = Convert.ToInt32(GetPoints.getPoints.PlayPoints.text);
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
                GetPoints.getPoints.numberValues.Add("10", NumberPoints.ten);
                GetPoints.getPoints.numberValues.Add("11", NumberPoints.eleven);

                GetPoints.getPoints.repeat = GetPoints.getPoints.baitList;


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
            // request.certificateHandler = new BypassCertificate();
            request.timeout = 100;
            yield return request.SendWebRequest();
            Debug.LogError(">>> " + request.downloadHandler.text);

            if (request.responseCode == 200)
            {
                GetWinNumber number = JsonUtility.FromJson<GetWinNumber>(request.downloadHandler.text);

                GetPoints.gotNumber = number.number;
                GetPoints.gotSymbol = number.number;
                GetPoints.gotTime = number.time;
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
            // request.certificateHandler = new BypassCertificate();
            request.timeout = 100;
            yield return request.SendWebRequest();
            Debug.LogError("Win report " + request.downloadHandler.text);

            if (request.responseCode == 200)
            {
                GetPoints.getPoints.MainPoints.text = request.downloadHandler.text;
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
            lowest.Add(NumberPoints.ten);
            lowest.Add(NumberPoints.eleven);



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
            if (NumberPoints.ten == gotNumber)
            {
                mins.Add(10);
            }
            if (NumberPoints.eleven == gotNumber)
            {
                mins.Add(11);
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