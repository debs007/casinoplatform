using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Compression.Zip;
using System.IO;
using ceTe.DynamicPDF.Printing;
using ceTe.DynamicPDF;
using System.Drawing;
using PdfiumViewer;

namespace TwelveCards
{
    //using ExclusivePrinting;

    //using System.Drawing.Printing;

    public class GetPoints : MonoBehaviour
    {
        public static GetPoints getPoints;
        //public UnityEngine.UI.Image[] ChangableSprites;
        int ChipValue, MainPlayValue = 0;
        private Quaternion _targetRotation = Quaternion.identity;
        public DetectNumber detect, detect2;
        public Vector3[] NumbersPositions;
        public Vector3[] SymbolPosition;
        public bool AllowRotation = false, ChipSelected = false, SetPoisitionNow = false, SetPoisitionNow2 = false;
        public GameObject NumberPanel, NumberPanel2;
        GameObject _GlobalChipHolder;
        public List<int> baitList = new List<int>();
        [SerializeField]
        [Range(0f, 10000f)]
        float _Speed = 500f;
        float _SpeedTs = 90f;
        public GameObject BaitNumberPanelParent;
        internal static string gotTime;
        float time = 0;
        public Text PlayPoints, MainPoints, Wintext;
        Coroutine InstanceHolder;
        bool WinDeclared = false;
        public static bool Is_Spinning = false;
        public int tokensWon, won_on_number;
        public int multiplication = 10;
        public GameObject front_msg_text, DoubleTextPlay, DoubleTextWin;
        public RectTransform fmsg_parent;
        UnityEngine.Color col;
        Coroutine resultCrt;
        float startRotation = 0;
        float endRotation = 0;

        public static string gotNumber, gotSymbol;
        [SerializeField]
        public List<int> repeat = new List<int>();
        public Button[] allButtonsToDisable;
        //static Button[] allButtonsToDisableStatic;
        public static bool allowBait = true;

        public Button[] AllJ;
        public Button[] AllK;
        public Button[] AllQ;
        public Button[] AllHeart;
        public Button[] AllSpades;
        public Button[] AllDiamond;
        public Button[] AllClub;
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
        public UnityEngine.UI.Image xNum;
        public Sprite[] xNumns;
        public int thZero = 0, thOne = 0, thTwo = 0, thThree = 0, thFour = 0, thFive = 0, thSix = 0, thSeven = 0, thEight = 0, thNine = 0, thTen = 0, thEleven = 0;

        public Transform pointValuesText;
        public GameObject ShowCard, Mover;
        Quaternion recordPos;
        public bool DoPrint;
        public Sprite Barcode;
        public Transform Diamond;

        public bool AllowPrint;
        //public PrintHandler handler;

        // Start is called before the first frame update
        void Start()
        {
            AllowPrint = true;
            // handler = new PrintHandler();
            //SymbolPosition = NumbersPositions;
            // NumbersPositions = ResultView.resultView.NumberPositions;
            // allowBait = true;
            getPoints = this;
            col = PlayPoints.color;

#if UNITY_ANDROID || UNITY_IOS
            foreach (GameObject obj in allToDisable)
            {
                obj.SetActive(false);
            }
#endif

            //allButtonsToDisableStatic = allButtonsToDisable;


        }

        public void ChangePrint(bool yes)
        {
            if (yes)
            {
                AllowPrint = true;
            }
            else
            {
                AllowPrint = false;
            }
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
        /// <summary>
        /// This sentstate variable makes sure when odds or evens or random number are selected at the same time , they don't call update bet instant funtion every time when the button is clicked
        /// </summary>
        bool sentState = true;
        public void SelectOdds()
        {
            if (!allowBait)
            {
                return;
            }
            //Clear(NumberDrawPanel);
            sentState = false;
            //foreach(Button btn in odds)
            //{
            //    btn.onClick.Invoke();
            //}
            sentState = true;
            GameController.instance.UpdateBetInstant(0, 0);
        }

        public void SelectAuto(string type)
        {
            Button[] auto = null;
            if (type == "j")
            {
                auto = AllJ;
            }
            else if (type == "k")
            {
                auto = AllK;
            }
            else if (type == "q")
            {
                auto = AllQ;
            }
            else if (type == "h")
            {
                auto = AllHeart;
            }
            else if (type == "s")
            {
                auto = AllSpades;
            }
            else if (type == "d")
            {
                auto = AllDiamond;
            }
            else if (type == "c")
            {
                auto = AllClub;
            }

            if (!allowBait)
            {
                return;
            }
            //Clear(NumberDrawPanel);
            sentState = false;
            foreach (Button btn in auto)
            {
                btn.onClick.Invoke();
            }
            sentState = true;
            GameController.instance.UpdateBetInstant(0, 0);
        }
        public void SelectEvens()
        {
            if (!allowBait)
            {
                return;
            }
            // Clear(NumberDrawPanel);
            sentState = false;
            //foreach (Button btn in evens)
            //{
            //    btn.onClick.Invoke();
            //}

            sentState = true;
            GameController.instance.UpdateBetInstant(0, 0);
        }

        public void SelectRandom(int lim)
        {
            if (!allowBait)
            {
                return;
            }
            Clear(NumberDrawPanel);
            List<int> usedNumbers = new List<int>();
            sentState = false;
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

            sentState = true;
            GameController.instance.UpdateBetInstant(0, 0);
        }

        void CreateTicket(int tid, int total, string allBets)
        {
            MyBetData myBets = new MyBetData();
            myBets.sessionNum = tid.ToString();
            myBets.sessionId = Constants.sessionData.session;
            myBets.time = GameController.instance.DrawTime;
            myBets.number = -1;
            myBets.win = "0";
            myBets.amount = total.ToString();
            myBets.bets = allBets;
            myBets.created_at = DateTime.Now.ToString();
            myBets.beforeBalance = total + Convert.ToInt32(MainPoints.text);
            myBets.afterBalance = Convert.ToInt32(MainPoints.text);

            Constants.tickets.Add(myBets);
            Stores stores = new Stores();
            stores.mybets = Constants.tickets;
            PlayerPrefs.SetString("tickets", JsonUtility.ToJson(stores));

        }

        public void PrintAction()
        {
            int total = thZero + thOne + thTwo + thThree + thFour + thFive + thSix + thSeven + thEight + thNine + thTen + thEleven;
            int[] allNumCounts = new int[] { thZero, thOne, thTwo, thThree, thFour, thFive, thSix, thSeven, thEight, thNine, thTen, thEleven };
            float pageY = 230f;
            int numCounter = 0;

            foreach (int sNum in allNumCounts)
            {
                if (sNum > 0)
                {
                    numCounter++;
                }

                if (numCounter > 10)
                {
                    pageY = 400f;
                }
                else if (numCounter > 8)
                {
                    pageY = 360f;
                }
                else if (numCounter > 6)
                {
                    pageY = 320f;
                }
                else if (numCounter > 4)
                {
                    pageY = 280f;
                }
                else if (numCounter > 2)
                {
                    pageY = 240f;
                }
            }

            if (total <= 0)
            {
                return;
            }
            int ticketId = UnityEngine.Random.Range(100000000, 999999999);
            string allBets = thZero + "|" + thOne + "|" + thTwo + "|" + thThree + "|" + thFour + "|" + thFive + "|" + thSix + "|" + thSeven + "|" + thEight + "|" + thNine + "|" + thTen + "|" + thEleven;
            CreateTicket(ticketId, total, allBets);
            //Create a new PDF document.
            PdfAlignmentStyle style = PdfAlignmentStyle.TopCenter;
            Syncfusion.Pdf.PdfDocument document = new Syncfusion.Pdf.PdfDocument();


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
            graphics.DrawString("GAME NAME : JEETO JOKER", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Terminal : " + Constants.userData.name, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Game ID : " + Constants.sessionData.sessionNum, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Ticket ID : " + ticketId, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Draw Time : " + GameController.instance.DrawTime, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Ticket Time : " + DateTime.Now.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            graphics.DrawString("Total : " + total, fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
            yVal += yIncr;
            int counter = 0;
            bool First = true;
            if (thZero > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }

                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                //yVal += yIncr * 2;
                graphics.DrawString("JH", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thZero.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                // yVal += yIncr * 2;
                counter++;

            }
            if (thOne > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                // yVal += yIncr * 2;
                graphics.DrawString("JS", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thOne.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                // yVal += yIncr * 2;
                counter++;

            }
            if (thTwo > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                // yVal += yIncr * 2;
                graphics.DrawString("JD", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thTwo.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                // yVal += yIncr * 2;
                counter++;

            }
            if (thThree > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                // yVal += yIncr * 2;
                graphics.DrawString("JC", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thThree.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                // yVal += yIncr * 2;
                counter++;

            }
            if (thFour > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                // yVal += yIncr * 2;
                graphics.DrawString("KH", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thFour.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                // yVal += yIncr * 2;
                counter++;


            }
            if (thFive > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                //yVal += yIncr * 2;
                graphics.DrawString("KS", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thFive.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                //yVal += yIncr * 2;
                counter++;

            }
            if (thSix > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                // yVal += yIncr * 2;
                graphics.DrawString("KD", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thSix.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                //yVal += yIncr * 2;
                counter++;

            }
            if (thSeven > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                // yVal += yIncr * 2;
                graphics.DrawString("KC", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thSeven.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                //yVal += yIncr * 2;
                counter++;

            }
            if (thEight > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                // yVal += yIncr * 2;
                graphics.DrawString("QH", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thEight.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                //yVal += yIncr * 2;
                counter++;

            }
            if (thNine > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                // 
                graphics.DrawString("QS", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thNine.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                //yVal += yIncr * 2;
                counter++;

            }
            if (thTen > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                // yVal += yIncr * 2;
                graphics.DrawString("QD", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thTen.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                //yVal += yIncr * 2;
                counter++;

            }
            if (thEleven > 0)
            {
                if (counter == 2)
                {
                    counter = 0;
                    yVal += yIncr * 2;
                    xVal = 22f;
                }
                else
                {
                    if (!First)
                        xVal += xIncr;
                    else
                        First = false;
                }
                graphics.DrawString("Item", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal));
                graphics.DrawString("Point", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal));
                graphics.DrawString("QC", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal, yVal + yIncr));
                graphics.DrawString(thEleven.ToString(), fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(xVal + xIncr, yVal + yIncr));
                xVal += xIncr;
                //yVal += yIncr * 2;
                counter++;

            }

            yVal += yIncr * 3;
            //byte[] bytes = File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath, "barcode.png"));
            byte[] bytes = Barcode.texture.EncodeToPNG();
            MemoryStream str = new MemoryStream(bytes);
            PdfImage img = PdfImage.FromStream(str);
            graphics.DrawImage(img, new Syncfusion.Drawing.PointF(20f, yVal), size: new Syncfusion.Drawing.SizeF(160f, 25f));
            yVal += yIncr * 3;
            graphics.DrawString("** Not for Sale **", fontGen, PdfBrushes.Black, new Syncfusion.Drawing.PointF(50f, yVal));
            //page.Section.PageSettings.Size = new Syncfusion.Drawing.SizeF(175f, yVal+20f);
            thZero = 0; thOne = 0; thTwo = 0; thThree = 0; thFour = 0; thFive = 0; thSix = 0; thSeven = 0; thEight = 0; thNine = 0; thTen = 0; thEleven = 0;

            foreach (Transform trn in pointValuesText)
            {
                trn.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = "0";
            }


            //Create the stream object.

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
            string path = Path.Combine(Application.persistentDataPath, $"jeetojoker{rand}.pdf");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.WriteAllBytes(path, stream.ToArray());


            //var printDoc = new PrintDocument();


            try
            {


                if (DoPrint && AllowPrint)
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

        public void DoAClear()
        {
            thZero = 0; thOne = 0; thTwo = 0; thThree = 0; thFour = 0; thFive = 0; thSix = 0; thSeven = 0; thEight = 0; thNine = 0; thTen = 0; thEleven = 0;

            foreach (Transform trn in pointValuesText)
            {
                trn.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = "0";
            }
        }

        public void TestButton()
        {

            int total = thZero + thOne + thTwo + thThree + thFour + thFive + thSix + thSeven + thEight + thNine + thTen + thEleven;
            if (total <= 0)
            {
                return;
            }

            GameController.instance.UpdateBetInstant(0, 0, true);
            //PrintingManager.Instance.PrintFiles(path);
            //PrintNow.Instance.StartPrint(path);

            //System.Diagnostics.Process.Start("Sample.pdf", @"C:\Program Files (x86)\Adobe\Acrobat DC\Acrobat\AcroRd32.exe");
        }

        public void PrintFromButton()
        {
            TestButton();
            return;



            string dataSet1 = "Session:" + Constants.sessionData.sessionNum + "\nDate-Time:" + DateTime.Now.ToString();

            //if(thZero > 0)
            //{
            //    dataSet1 += "\n\nJ❤ : " + thZero;
            //}
            //if (thOne > 0)
            //{
            //    dataSet1 += "\nJ❤ : " + thOne;
            //}
            //if (thTwo > 0)
            //{
            //    dataSet1 += "\nJ♦ : " + thTwo;
            //}
            //if (thThree > 0)
            //{
            //    dataSet1 += "\nJ♣ : " + thThree;
            //}
            //if (thFour > 0)
            //{
            //    dataSet1 += "\nK❤ : " + thFour;
            //}
            //if (thFive > 0)
            //{
            //    dataSet1 += "\nK♠ : " + thFive;
            //}
            //if (thSix > 0)
            //{
            //    dataSet1 += "\nK♦ : " + thSix;
            //}
            //if (thSeven > 0)
            //{
            //    dataSet1 += "\nK♣ : " + thSeven;
            //}
            //if (thEight > 0)
            //{
            //    dataSet1 += "\nQ❤ : " + thEight;
            //}
            //if (thNine > 0)
            //{
            //    dataSet1 += "\nQ♠ : " + thNine;
            //}
            //if (thTen > 0)
            //{
            //    dataSet1 += "\nQ♦ : " + thTen;
            //}
            //if (thEleven > 0)
            //{
            //    dataSet1 += "\nQ♣ : " + thEleven;
            //}

            //dataSet1 += "\n-------------\nTotal: " + total;

            string header = "";
            string[] data = new string[2];
            data[0] = dataSet1;
            data[1] = header;
            PrintNow.Instance.StartPrint(data);

            thZero = 0; thOne = 0; thTwo = 0; thThree = 0; thFour = 0; thFive = 0; thSix = 0; thSeven = 0; thEight = 0; thNine = 0; thTen = 0; thEleven = 0;

            foreach (Transform trn in pointValuesText)
            {
                trn.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = "0";
            }
        }
        // Update is called once per frame
        void Update()
        {


            //if (Convert.ToInt32(PlayPoints.text) > Convert.ToInt32(MainPoints.text))
            //{
            //    PlayPoints.UnityEngine.Color = UnityEngine.Color.red;
            //}
            //else
            //    PlayPoints.UnityEngine.Color = col;

            if (Input.GetKeyDown(KeyCode.P))
            {
                //int total = thZero + thOne + thTwo + thThree + thFour + thFive + thSix + thSeven + thEight + thNine;
                //string dataSet1 = "Session :  " + Constants.sessionData.sessionNum+"\nDate Time : "+DateTime.Now.ToString()+"\n\nZero : " + thZero + "\nOne : " + thOne + "\nTwo : " + thTwo + "\nThree : " + thThree + "\nFour : " + thFour + "\nFive : " + thFive + "\nSix : " + thSix + "\nSeven : " + thSeven + "\nEight : " + thEight + "\nNine : " + thNine+"\n-------------\nTotal: "+total;
                //string header = "";
                //string[] data = new string[2];
                //data[0] = dataSet1;
                //data[1] = header;
                //PrintNow.Instance.StartPrint(data);

                //thZero = 0; thOne = 0; thTwo = 0; thThree = 0; thFour = 0; thFive = 0; thSix = 0; thSeven = 0; thEight = 0; thNine = 0; thTen = 0; thEleven = 0;

                //foreach (Transform trn in pointValuesText)
                //{
                //    trn.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = "0";
                //}

                TestButton();
            }

            if (GameController.has_network)
            {


                if (WinDeclared)
                {
                    WinDeclared = false;
                    SetPoisitionNow = false;
                    SetPoisitionNow2 = false;
                    StopCoroutine(InstanceHolder);
                }
                //        UnityEngine.Debug.Log(NumberPanel.GetComponent<Rigidbody2D>().angularVelocity);
                if (AllowRotation)
                {
                    SetPoisitionNow = false;
                    SetPoisitionNow2 = false;

                    WinHit = false;
                    detect.StopDetecting = false;
                    detect2.StopDetecting2 = false;
                    time += Time.deltaTime;
                    //UnityEngine.Debug.Log(time);
                    NumberPanel.transform.Rotate(new Vector3(0, 0, -1f) * _Speed * Time.deltaTime);
                    NumberPanel2.transform.Rotate(new Vector3(0, 0, 1f) * _Speed * Time.deltaTime);


                    // _Speed -= 5f;


                    if (time >= 2f)
                    {
                        //Debug.LogError("stopper : " + Constants.sessionData.stoper);
                        _Speed = (float)Constants.sessionData.fSpeed;
                    }

                    //if (time >= 3.0f)
                    //{
                    //    _Speed = 90f;

                    //    //UnityEngine.Debug.Log("Speed is: "+_Speed);
                    //}


                    if (_Speed <= Constants.sessionData.fSpeed)
                    {
                        //UnityEngine.Debug.Log("The Speed have been set to lowest");
                        AllowRotation = false;
                        SetPoisitionNow = true;
                        SetPoisitionNow2 = true;
                        //_targetRotation = Quaternion.Euler(NumbersPositions[0]);
                        // NumberPanel.transform.rotation = _targetRotation;

                        endRotation = _targetRotation.eulerAngles.z;
                        startRotation = NumberPanel.transform.eulerAngles.z;
                        // InstanceHolder = StartCoroutine(GetWinNumber());
                    }

                }
                if (SetPoisitionNow)
                {

                    NumberPanel.transform.Rotate(new Vector3(0, 0, -1f) * Constants.sessionData.fSpeed * Time.deltaTime);
                    NumberPanel2.transform.Rotate(new Vector3(0, 0, 1f) * Constants.sessionData.sSpeed * Time.deltaTime);
                    //detect.StopDetecting = true;
                    //endRotation += _SpeedTs * Time.deltaTime;
                    ////NumberPanel.transform.rotation = Quaternion.Euler(0f,0f, Mathf.Lerp(NumberPanel.transform.rotation.z, _targetRotation.z, _SpeedTs * Time.deltaTime));

                    //float eulerRot = Mathf.Lerp(startRotation, endRotation, .5f);
                    //NumberPanel.transform.eulerAngles = new Vector3(0, 0, eulerRot);



                    //if (true && gotNumber != "")
                    //{
                    //   // print("Not zero");
                    //    NumberPanel.transform.Rotate(new Vector3(0, 0, -1f) * _Speed * Time.deltaTime);
                    //    recordPos = NumberPanel.transform.rotation;
                    //}

                    if (true && gotNumber != "")
                    {
                        _targetRotation = Quaternion.Euler(NumbersPositions[Convert.ToInt32(gotNumber)]);
                        //  NumberPanel.transform.rotation = Quaternion.Lerp(NumberPanel.transform.rotation, _targetRotation, 5f * Time.deltaTime);
                        //NumberPanel.transform.rotation = Quaternion.Euler(NumbersPositions[Convert.ToInt32(gotNumber)]);
                        SetPoisitionNow = false;

                        if (SetPoisitionNow2)
                        {
                            SetPoisitionNow2 = false;
                            StartCoroutine(StopFirstWheel());
                            StartCoroutine(StopSymbolWheel());
                        }

                        //if(NumberPanel.transform.rotation.z != _targetRotation.z)
                        //{
                        //    NumberPanel.transform.Rotate(new Vector3(0, 0, -1f) * _Speed * Time.deltaTime);
                        //}

                    }



                }
            }
        }
        bool firstWheelStopped = false;
        IEnumerator StopFirstWheel()
        {
            firstWheelStopped = false;
            //yield return new WaitForSeconds(3f);
            UnityEngine.Debug.LogError("Calling round");
            Quaternion recordP = NumberPanel.transform.rotation;
            Vector3 targetRotation = NumbersPositions[Convert.ToInt32(gotNumber)];
            float mainVal = 0f;
            float time = 1f;
            float fromAngle = NumberPanel.transform.rotation.z;
            float toAngle = targetRotation.z;
            //if (toAngle < fromAngle)
            //{
            //    toAngle += 360f; // Prevents backward rotation
            //}
            while (true)
            {
                //if(detect.LastObject.name == gotNumber)
                //{
                //    detect.StopDetecting = true;
                //    NumberPanel.transform.rotation = Quaternion.RotateTowards(NumberPanel.transform.rotation, targetRotation, _Speed * Time.deltaTime);
                //}
                //else
                //var currentRotation = NumberPanel.transform.rotation.eulerAngles;

                //var angleDiff = Mathf.DeltaAngle(currentRotation.z, targetRotation.z);
                //NumberPanel.transform.Rotate(new Vector3(0, 0, -1f), angleDiff * Constants.sessionData.fSpeed * Time.deltaTime,Space.World);

                //yield return new WaitForSeconds(0.016f);
                //mainVal += 0.016f;
                // float diff = Quaternion.Angle(NumberPanel.transform.rotation, targetRotation);
                ////float diff = Vector3.Distance()
                //if (diff <= 2f)
                //    break;
                mainVal += Time.deltaTime;

                //Quaternion currentRotation = NumberPanel.transform.rotation;
                //float zRotation = Mathf.LerpAngle(fromAngle, toAngle, mainVal / time);

                //var angleDiff = Mathf.DeltaAngle(currentRotation.z, targetRotation.z);

                //else


                NumberPanel.transform.Rotate(new Vector3(0, 0, -1f) * Constants.sessionData.fSpeed * Time.deltaTime);
                //NumberPanel.transform.rotation = Quaternion.Euler(0, 0, -zRotation);


                // 
                yield return null;


                float diff = Quaternion.Angle(NumberPanel.transform.rotation, Quaternion.Euler(targetRotation));
                // print("f wheel diff " + diff);
                if (diff <= 3f)
                {
                    break;
                }


            }

            NumberPanel.transform.rotation = Quaternion.Euler(targetRotation);
            yield return new WaitForSeconds(1f);
            firstWheelStopped = true;
            //string cardGet = Constants.cards[Convert.ToInt32(gotNumber)];
            //string[] allCr = cardGet.Split(' ');
            //Mover.SetActive(false);
            //ShowCard.SetActive(true);
            //.transform.GetChild(0).gameObject.GetComponent<UnityEngine.UI.Image>().sprite = ResultView.CardGet[allCr[0]];
            //.transform.GetChild(1).gameObject.GetComponent<UnityEngine.UI.Image>().sprite = ResultView.symbolGet[allCr[1]];
            yield break;
        }

        IEnumerator StopSymbolWheel()
        {
            //yield return new WaitForSeconds(3f);
            UnityEngine.Debug.LogError("Calling round");
            Quaternion recordP = NumberPanel2.transform.rotation;
            Vector3 targetRotation = SymbolPosition[Convert.ToInt32(gotSymbol)];
            float mainVal = 0f;
            string cardGet = Constants.cards[Convert.ToInt32(gotNumber)];
            string[] allCr = cardGet.Split(' ');
            float time = 1.8f;
            float fromAngle = NumberPanel2.transform.rotation.z;
            float toAngle = targetRotation.z;

            while (true)
            {

                //if(!firstWheelStopped)
                //{
                //    NumberPanel2.transform.Rotate(new Vector3(0, 0, 1f) * Constants.sessionData.sSpeed * Time.deltaTime);
                //    yield return new WaitForSeconds(0.016f);
                //}
                //else
                //{
                //    Quaternion currentRotation = NumberPanel2.transform.rotation;

                //    //var angleDiff = Mathf.DeltaAngle(currentRotation.z, targetRotation.z);

                //    //else
                //    Quaternion targe = Quaternion.LookRotation(targetRotation, new Vector3(0, 0, 1f));

                NumberPanel2.transform.Rotate(new Vector3(0, 0, 1f) * Constants.sessionData.sSpeed * Time.deltaTime);
                //    NumberPanel2.transform.rotation = Quaternion.RotateTowards(currentRotation, targe, Constants.sessionData.sSpeed * Time.deltaTime);


                //    // 
                //    yield return new WaitForSeconds(0.016f);
                //    mainVal += 0.016f;


                //}

                mainVal += Time.deltaTime;

                //Quaternion currentRotation = NumberPanel2.transform.rotation;
                // float zRotation = Mathf.LerpAngle(fromAngle, toAngle, mainVal / time);

                //var angleDiff = Mathf.DeltaAngle(currentRotation.z, targetRotation.z);

                //else


                //NumberPanel2.transform.Rotate(new Vector3(0, 0, 1f) * Constants.sessionData.sSpeed * Time.deltaTime);
                // NumberPanel2.transform.rotation = Quaternion.Euler(0, 0, zRotation);


                // 
                yield return null;
                float diff = Quaternion.Angle(NumberPanel2.transform.rotation, Quaternion.Euler(targetRotation));
                // print("s wheel diff " + diff);
                if (diff <= 3f && firstWheelStopped)
                    break;


            }

            NumberPanel2.transform.rotation = Quaternion.Euler(targetRotation);
            SpinWheelAudio.Stop();

            Mover.SetActive(false);
            ShowCard.SetActive(true);
            ShowCard.transform.GetChild(0).gameObject.GetComponent<UnityEngine.UI.Image>().sprite = ResultView.CardGet[allCr[0]];
            ShowCard.transform.GetChild(1).gameObject.GetComponent<UnityEngine.UI.Image>().sprite = ResultView.symbolGet[allCr[1]];
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
            yield break;
        }

        private bool ClearVal = false;
        public void AllowNumberClear()
        {
            ClearVal = true;
        }

        public void ClearNumber(GameObject num)
        {
            if (!allowBait)
            {
                return;
            }
            int amountToSubstact = 0;
            num.transform.GetChild(0).gameObject.GetComponent<Text>().text = "0";
            if (num.name == "0")
            {
                amountToSubstact = NumberPoints.zero;
                NumberPoints.zero = 0;
                thZero = 0;
                baitList.Remove(0);
            }

            if (num.name == "1")
            {
                amountToSubstact = NumberPoints.one;
                NumberPoints.one = 0;
                baitList.Remove(1);
                thOne = 0;
            }
            if (num.name == "2")
            {
                amountToSubstact = NumberPoints.two;
                NumberPoints.two = 0;
                baitList.Remove(2);
                thTwo = 0;
            }
            if (num.name == "3")
            {
                amountToSubstact = NumberPoints.three;
                NumberPoints.three = 0;
                baitList.Remove(3);
                thThree = 0;
            }
            if (num.name == "4")
            {
                amountToSubstact = NumberPoints.four;
                NumberPoints.four = 0;
                baitList.Remove(4);
                thFour = 0;
            }
            if (num.name == "5")
            {
                amountToSubstact = NumberPoints.five;
                NumberPoints.five = 0;
                baitList.Remove(5);
                thFive = 0;
            }
            if (num.name == "6")
            {
                amountToSubstact = NumberPoints.six;
                NumberPoints.six = 0;
                baitList.Remove(6);
                thSix = 0;
            }
            if (num.name == "7")
            {
                amountToSubstact = NumberPoints.seven;
                NumberPoints.seven = 0;
                baitList.Remove(7);
                thSeven = 0;
            }
            if (num.name == "8")
            {
                amountToSubstact = NumberPoints.eight;
                NumberPoints.eight = 0;
                baitList.Remove(8);
                thEight = 0;
            }
            if (num.name == "9")
            {
                amountToSubstact = NumberPoints.nine;
                NumberPoints.nine = 0;
                baitList.Remove(9);
                thNine = 0;
            }
            if (num.name == "10")
            {
                amountToSubstact = NumberPoints.ten;
                NumberPoints.ten = 0;
                baitList.Remove(10);
                thTen = 0;
            }
            if (num.name == "11")
            {
                amountToSubstact = NumberPoints.eleven;
                NumberPoints.eleven = 0;
                baitList.Remove(11);
                thEleven = 0;
            }

            PlayPoints.text = (Convert.ToInt32(PlayPoints.text) - amountToSubstact).ToString();
            MainPoints.text = (Convert.ToInt32(MainPoints.text) + amountToSubstact).ToString();
        }

        public int GetTotalOfCurrentBet()
        {
            int tp = thZero + thOne + thTwo + thThree + thFour + thFive + thSix + thSeven + thEight + thNine + thTen + thEleven;
            return tp;
        }
        int prev0, prev1, prev2, prev3, prev4, prev5, prev6, prev7, prev8, prev9;
        public void MakeDouble()
        {
            if (!allowBait)
            {
                return;
            }

            if (GetTotalOfCurrentBet() == 0)
            {
                return;
            }
            if (Convert.ToInt32(PlayPoints.text) <= Convert.ToInt32(MainPoints.text))
            {
                multiplication = 10;
                //DoubleTextWin.SetActive(true);
                //DoubleTextPlay.SetActive(true);
                MainPoints.text = (Convert.ToInt32(MainPoints.text) - (Convert.ToInt32(PlayPoints.text))).ToString();
                PlayPoints.text = (Convert.ToInt32(PlayPoints.text) * 2).ToString();



                foreach (GameObject num in allNumbersButton)
                {
                    if (num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text != "0")
                    {
                        if (baitList.Contains(Convert.ToInt32(num.name)))
                        {
                            if (num.name == "0")
                            {
                                prev0 = NumberPoints.zero;
                                NumberPoints.zero *= 2;
                                thZero *= 2;
                                //prev0 = NumberPoints.zero - prev0;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.zero.ToString();
                                //GameController.instance.UpdateBetInstant(0, prev0);
                            }
                            if (num.name == "1")
                            {
                                UnityEngine.Debug.LogError("np 1" + NumberPoints.one);
                                prev1 = NumberPoints.one;
                                NumberPoints.one *= 2;
                                thOne *= 2;
                                //prev1 = NumberPoints.one - prev1;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.one.ToString();
                                UnityEngine.Debug.LogError("np 1 re" + NumberPoints.one);

                            }
                            if (num.name == "2")
                            {
                                prev2 = NumberPoints.two;
                                NumberPoints.two *= 2;
                                thTwo *= 2;
                                //prev2 = NumberPoints.two - prev2;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.two.ToString();
                                // GameController.instance.UpdateBetInstant(2, prev2);
                            }
                            if (num.name == "3")
                            {
                                prev3 = NumberPoints.three;
                                NumberPoints.three *= 2;
                                thThree *= 2;
                                //prev3 = NumberPoints.three - prev3;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.three.ToString();
                                //GameController.instance.UpdateBetInstant(3, prev3);
                            }
                            if (num.name == "4")
                            {
                                prev4 = NumberPoints.four;
                                NumberPoints.four *= 2;
                                thFour *= 2;
                                //prev4 = NumberPoints.four - prev4;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.four.ToString();
                                //GameController.instance.UpdateBetInstant(4, prev4);
                            }
                            if (num.name == "5")
                            {
                                prev5 = NumberPoints.five;
                                NumberPoints.five *= 2;
                                thFive *= 2;
                                //prev5 = NumberPoints.five - prev5;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.five.ToString();
                                // GameController.instance.UpdateBetInstant(5, prev5);
                            }
                            if (num.name == "6")
                            {
                                prev6 = NumberPoints.six;
                                NumberPoints.six *= 2;
                                thSix *= 2;
                                //prev6 = NumberPoints.six - prev6;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.six.ToString();
                                //GameController.instance.UpdateBetInstant(6, prev6);
                            }
                            if (num.name == "7")
                            {
                                prev7 = NumberPoints.seven;
                                NumberPoints.seven *= 2;
                                thSeven *= 2;
                                //prev7 = NumberPoints.seven - prev7;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.seven.ToString();
                                //GameController.instance.UpdateBetInstant(7, prev7);
                            }
                            if (num.name == "8")
                            {
                                prev8 = NumberPoints.eight;
                                NumberPoints.eight *= 2;
                                thEight *= 2;
                                //prev8 = NumberPoints.eight - 8;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.eight.ToString();
                                //GameController.instance.UpdateBetInstant(8, prev8);
                            }
                            if (num.name == "9")
                            {
                                prev9 = NumberPoints.nine;
                                NumberPoints.nine *= 2;
                                thNine *= 2;
                                // prev9 = NumberPoints.nine - prev9;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.nine.ToString();
                                //GameController.instance.UpdateBetInstant(9, prev9);
                            }
                            if (num.name == "10")
                            {
                                prev10 = NumberPoints.ten;
                                NumberPoints.ten *= 2;
                                thTen *= 2;
                                // prev9 = NumberPoints.nine - prev9;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.ten.ToString();
                                //GameController.instance.UpdateBetInstant(9, prev9);
                            }
                            if (num.name == "11")
                            {
                                prev11 = NumberPoints.eleven;
                                NumberPoints.eleven *= 2;
                                thEleven *= 2;
                                // prev9 = NumberPoints.nine - prev9;
                                num.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = NumberPoints.eleven.ToString();
                                //GameController.instance.UpdateBetInstant(9, prev9);
                            }
                        }
                    }
                }

                GameController.instance.UpdateBetInstant(0, 0);
                //popupmsg("Doubled");
            }
            else
            {
                ScreenFlowHandler.screenFlow.ShowErr();
                return;
            }
        }

        public void CancleDouble()
        {
            if (multiplication == 9 && !Is_Spinning)
            {
                multiplication = 10;
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
            UnityEngine.Debug.Log("Checking for: " + check);
            foreach (int n in baitList)
                UnityEngine.Debug.Log("The current list is: " + n);
            if (baitList.Contains(check))
            {
                UnityEngine.Debug.Log("User won among list");
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
                trn.gameObject.GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(255f, 255f, 255f, 1f);
            }
            while (true)
            {
                yield return new WaitForSeconds(.2f);
                foreach (Transform trn in allLightHolder)
                {
                    trn.gameObject.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.red;
                }
                yield return new WaitForSeconds(.2f);
                foreach (Transform trn in allLightHolder)
                {
                    trn.gameObject.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.yellow;
                }
                yield return new WaitForSeconds(.2f);
                foreach (Transform trn in allLightHolder)
                {
                    trn.gameObject.GetComponent<UnityEngine.UI.Image>().color = UnityEngine.Color.green;
                }
            }


        }

        void SetWinandLossValues(bool status)
        {
            int MainPointsNow = Convert.ToInt32(MainPoints.text);
            multiplication *= GameController.selectedXVal;
            tokensWon = 0;

            if (status)
                switch (gotNumber)
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

                    case "10":
                        MainPointsNow += NumberPoints.ten * multiplication;
                        tokensWon = NumberPoints.ten * multiplication;
                        won_on_number = 10;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);

                        break;

                    case "11":
                        MainPointsNow += NumberPoints.eleven * multiplication;
                        tokensWon = NumberPoints.eleven * multiplication;
                        won_on_number = 11;
                        //MainPointsNow -= Convert.ToInt32(PlayPoints.text);

                        break;

                }
            else
            {
                //MainPointsNow -= Convert.ToInt32(PlayPoints.text);
                tokensWon = 0;
                won_on_number = -90;
            }


            //NAnim.enabled = true;
            NAnim.SetBool("Hit", true);
            //NNumber.text = gotNumber.ToString();
            // NNumber.gameObject.SetActive(true);
            MainPoints.text = MainPointsNow.ToString();

            if (status)
                Wintext.text = (tokensWon).ToString();

            GameObject obj = null;
            if (status)
            {
                //obj = BaitNumberPanelParent.transform.Find(detect.LastObject.name).gameObject;
                //obj.GetComponent<Animator>().SetBool("Play", true);

                popupmsg("+" + tokensWon.ToString());


            }
            DoubleTextWin.SetActive(false);
            DoubleTextPlay.SetActive(false);

            foreach (MyBetData mb in Constants.tickets)
            {
                //Debug.LogError("s1:" + mb.sessionId + " " + "s2:" + Constants.sessionData.session);
                if (mb.sessionId == Constants.sessionData.session)
                {

                    if (won_on_number != -90)
                    {
                        string[] ab = mb.bets.Split('|');
                        string valueGot = ab[(won_on_number)];
                        int ticketWinning = Convert.ToInt32(valueGot) * multiplication;
                        mb.win = ticketWinning.ToString();
                    }
                    else
                    {
                        mb.win = "0";
                    }


                    mb.number = Convert.ToInt32(gotNumber);
                }
            }
            multiplication = 10;
            Stores stores = new Stores();
            stores.mybets = Constants.tickets;
            PlayerPrefs.SetString("tickets", JsonUtility.ToJson(stores));

            StartCoroutine(ShowWinOrLossMsg(obj));



        }

        IEnumerator ShowWinOrLossMsg(GameObject obj)
        {
            yield return new WaitForSeconds(1f);
            //popupmsg("-" + (Convert.ToInt32(PlayPoints.text)).ToString());
            if (Wintext.text != "0")
            {
                youWinMsg.SetActive(true);
                YouWinText.text = tokensWon.ToString();
            }
            Glow.SetActive(true);
            yield return new WaitForSeconds(1f);
            GameController.TimerStarted = false;
            yield return new WaitForSeconds(2f);
            Glow.SetActive(false);
            if (obj != null)
                obj.GetComponent<Animator>().SetBool("Play", false);
            multiplication = 10;
            allowBait = true;
            Clear(NumberDrawPanel, true);
            if (numberFlashLight != null)
            {
                StopCoroutine(numberFlashLight);
                numberFlashLight = null;
            }

            foreach (Transform trn in allLightHolder)
            {
                trn.gameObject.GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(255f, 255f, 255f, 0f);
            }

            BlackLayover.SetActive(false);
            ShowCard.SetActive(false);
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
            Obj.transform.parent.GetComponent<UnityEngine.UI.Image>().enabled = true;
            if (_GlobalChipHolder != null)
            {
                _GlobalChipHolder.transform.parent.GetComponent<UnityEngine.UI.Image>().enabled = false;
            }
            _GlobalChipHolder = Obj;
        }

        public void GetNumbersBait(GameObject num)
        {
            if (ChipSelected && allowBait)
            {

                if (ClearVal)
                {
                    ClearVal = false;
                    ClearNumber(num);
                    return;
                }

                if (Convert.ToInt32(MainPoints.text) < ChipValue)
                {
                    print("err");
                    ScreenFlowHandler.screenFlow.ShowErr();
                    return;
                }
                UnityEngine.Debug.Log("Chip value is:" + ChipValue);
                MainPlayValue = Convert.ToInt32(PlayPoints.text);
                MainPlayValue += ChipValue;
                Text valTxt = num.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>();

                PlayPoints.text = MainPlayValue.ToString();
                MainPoints.text = (Convert.ToInt32(MainPoints.text) - ChipValue).ToString();
                // MainPoints.text = (Convert.ToInt32(MainPoints.text) - ChipValue).ToString();
                int selectedNum = -1;
                switch (num.name)
                {
                    case "0":
                        NumberPoints.zero += ChipValue;
                        if (!baitList.Contains(0))
                            baitList.Add(0);
                        selectedNum = 0;
                        thZero += ChipValue;
                        valTxt.text = thZero.ToString();
                        break;
                    case "1":
                        NumberPoints.one += ChipValue;
                        if (!baitList.Contains(1))
                            baitList.Add(1);
                        selectedNum = 1;
                        thOne += ChipValue;
                        valTxt.text = thOne.ToString();
                        break;
                    case "2":
                        NumberPoints.two += ChipValue;
                        if (!baitList.Contains(2))
                            baitList.Add(2);
                        selectedNum = 2;
                        thTwo += ChipValue;
                        valTxt.text = thTwo.ToString();
                        break;
                    case "3":
                        NumberPoints.three += ChipValue;
                        if (!baitList.Contains(3))
                            baitList.Add(3);
                        selectedNum = 3;
                        thThree += ChipValue;
                        valTxt.text = thThree.ToString();
                        break;
                    case "4":
                        NumberPoints.four += ChipValue;
                        if (!baitList.Contains(4))
                            baitList.Add(4);
                        selectedNum = 4;
                        thFour += ChipValue;
                        valTxt.text = thFour.ToString();
                        break;
                    case "5":
                        NumberPoints.five += ChipValue;
                        if (!baitList.Contains(5))
                            baitList.Add(5);
                        selectedNum = 5;
                        thFive += ChipValue;
                        valTxt.text = thFive.ToString();
                        break;
                    case "6":
                        NumberPoints.six += ChipValue;
                        if (!baitList.Contains(6))
                            baitList.Add(6);
                        selectedNum = 6;
                        thSix += ChipValue;
                        valTxt.text = thSix.ToString();
                        break;
                    case "7":
                        NumberPoints.seven += ChipValue;
                        if (!baitList.Contains(7))
                            baitList.Add(7);
                        selectedNum = 7;
                        thSeven += ChipValue;
                        valTxt.text = thSeven.ToString();
                        break;
                    case "8":
                        NumberPoints.eight += ChipValue;
                        if (!baitList.Contains(8))
                            baitList.Add(8);
                        selectedNum = 8;
                        thEight += ChipValue;
                        valTxt.text = thEight.ToString();
                        break;
                    case "9":
                        NumberPoints.nine += ChipValue;
                        if (!baitList.Contains(9))
                            baitList.Add(9);
                        selectedNum = 9;
                        thNine += ChipValue;
                        valTxt.text = thNine.ToString();
                        break;

                    case "10":
                        NumberPoints.ten += ChipValue;
                        if (!baitList.Contains(10))
                            baitList.Add(10);
                        selectedNum = 10;
                        thTen += ChipValue;
                        valTxt.text = thTen.ToString();
                        break;
                    case "11":
                        NumberPoints.eleven += ChipValue;
                        if (!baitList.Contains(11))
                            baitList.Add(11);
                        selectedNum = 11;
                        thEleven += ChipValue;
                        valTxt.text = thEleven.ToString();
                        break;

                }
                if (sentState)
                    GameController.instance.UpdateBetInstant(selectedNum, ChipValue);


            }
        }

        Coroutine Xcrt = null;
        private int prev10;
        private int prev11;

        public void SpinWheel()
        {
            //baitList.Clear();
            if (!Is_Spinning && GameController.has_network)
            {
                //if (Convert.ToInt32(PlayPoints.text) > Convert.ToInt32(MainPoints.text))
                //{
                //    ScreenFlowHandler.screenFlow.ShowErr();
                //    return;
                //}
                Is_Spinning = true;
                //_Speed = (float)UnityEngine.Random.Range(20, 30);
                _Speed = 400f;
                time = 0f;
                AllowRotation = true;
                Xcrt = StartCoroutine(ShowXVal());
                SpinWheelAudio.Play();
                Mover.SetActive(true);

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

                if (counter == 10)
                {
                    counter = 0;
                }

                counter++;

                //waitTime += 0.05f;
            }
        }

        public void CLR(GameObject Traverse)
        {
            // Clear(Traverse);
            foreach (Transform trn in Traverse.transform)
            {
                trn.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = "0";
            }

            NumberPoints.zero -= thZero; NumberPoints.one -= thOne; NumberPoints.two -= thTwo; NumberPoints.three -= thThree; NumberPoints.four -= thFour; NumberPoints.five -= thFive; NumberPoints.six -= thSix; NumberPoints.seven -= thSeven;
            NumberPoints.eight -= thEight; NumberPoints.nine -= thNine; NumberPoints.ten -= thTen; NumberPoints.eleven -= thEleven;

            int totalTemp = thZero + thOne + thTwo + thThree + thFour + thFive + thSix + thSeven + thEight + thNine + thTen + thEleven;
            MainPlayValue -= totalTemp;

            thZero = 0; thOne = 0; thTwo = 0; thThree = 0; thFour = 0; thFive = 0; thSix = 0; thSeven = 0; thEight = 0; thNine = 0; thTen = 0; thEleven = 0;

            int toal = NumberPoints.zero + NumberPoints.one + NumberPoints.two + NumberPoints.three + NumberPoints.four + NumberPoints.five + NumberPoints.six + NumberPoints.seven + NumberPoints.eight + NumberPoints.nine + NumberPoints.ten + NumberPoints.eleven;
            MainPlayValue = toal;
            PlayPoints.text = MainPlayValue.ToString();
            MainPoints.text = (Convert.ToInt32(MainPoints.text) + totalTemp).ToString();

            if (toal <= 0)
                baitList.Clear();

            multiplication = 10;
            DoubleTextWin.SetActive(false);
            DoubleTextPlay.SetActive(false);

            GameController.instance.CheckRepeat();
            GameController.instance.UpdateBetInstant(0, 0);
        }


        public void Clear(GameObject Traverse, bool onlyClear = false, bool SpecialAccess = false)
        {
            if (!allowBait && !SpecialAccess)
            {
                return;
            }
            foreach (Transform trn in Traverse.transform)
            {
                trn.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = "0";
            }
            //MainPoints.text = "1000";
            if (!onlyClear)
            {

                MainPoints.text = (Convert.ToInt32(MainPoints.text) + Convert.ToInt32(PlayPoints.text)).ToString();
            }

            PlayPoints.text = "0";
            MainPlayValue = 0;




            NumberPoints.zero = 0; NumberPoints.one = 0; NumberPoints.two = 0; NumberPoints.three = 0; NumberPoints.four = 0; NumberPoints.five = 0; NumberPoints.six = 0; NumberPoints.seven = 0;
            NumberPoints.eight = 0; NumberPoints.nine = 0; NumberPoints.ten = 0; NumberPoints.eleven = 0;

            thZero = 0; thOne = 0; thTwo = 0; thThree = 0; thFour = 0; thFive = 0; thSix = 0; thSeven = 0; thEight = 0; thNine = 0; thTen = 0; thEleven = 0;

            int toal = NumberPoints.zero + NumberPoints.one + NumberPoints.two + NumberPoints.three + NumberPoints.four + NumberPoints.five + NumberPoints.six + NumberPoints.seven + NumberPoints.eight + NumberPoints.nine + NumberPoints.ten + NumberPoints.eleven;

            if (toal <= 0)
                baitList.Clear();

            multiplication = 10;
            DoubleTextWin.SetActive(false);
            DoubleTextPlay.SetActive(false);



        }

        public void Repeat()
        {
            if (!allowBait)
            {
                return;
            }

            if (Convert.ToInt32(MainPoints.text) < LastBait)
            {
                ScreenFlowHandler.screenFlow.ShowErr();
                return;
            }

            //Clear(NumberDrawPanel);

            foreach (KeyValuePair<string, int> numVal in numberValues)
            {
                GameObject obj = GameObject.FindGameObjectWithTag(numVal.Key);
                obj.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = numVal.Value.ToString();
                //obj.GetComponent<Button>().onClick.Invoke();
                MainPoints.text = (Convert.ToInt32(MainPoints.text) - numVal.Value).ToString();

                if (numVal.Key == "0")
                {
                    NumberPoints.zero = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(0))
                        baitList.Add(0);


                }
                if (numVal.Key == "1")
                {
                    NumberPoints.one = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(1))
                        baitList.Add(1);

                    //GameController.instance.UpdateBetInstant(1, NumberPoints.one);
                }
                if (numVal.Key == "2")
                {
                    NumberPoints.two = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(2))
                        baitList.Add(2);

                    // GameController.instance.UpdateBetInstant(2, NumberPoints.two);
                }

                if (numVal.Key == "3")
                {
                    NumberPoints.three = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(3))
                        baitList.Add(3);

                    // GameController.instance.UpdateBetInstant(3, NumberPoints.three);
                }

                if (numVal.Key == "4")
                {
                    NumberPoints.four = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(4))
                        baitList.Add(4);

                    //GameController.instance.UpdateBetInstant(4, NumberPoints.four);
                }
                if (numVal.Key == "5")
                {
                    NumberPoints.five = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(5))
                        baitList.Add(5);

                    //GameController.instance.UpdateBetInstant(5, NumberPoints.five);
                }
                if (numVal.Key == "6")
                {
                    NumberPoints.six = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(6))
                        baitList.Add(6);

                    //GameController.instance.UpdateBetInstant(6, NumberPoints.six);
                }

                if (numVal.Key == "7")
                {
                    NumberPoints.seven = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(7))
                        baitList.Add(7);

                    //GameController.instance.UpdateBetInstant(7, NumberPoints.seven);
                }

                if (numVal.Key == "8")
                {
                    NumberPoints.eight = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(8))
                        baitList.Add(8);

                    //GameController.instance.UpdateBetInstant(8, NumberPoints.eight);
                }
                if (numVal.Key == "9")
                {
                    NumberPoints.nine = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(9))
                        baitList.Add(9);

                    //GameController.instance.UpdateBetInstant(9, NumberPoints.nine);
                }
                if (numVal.Key == "10")
                {
                    NumberPoints.ten = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(10))
                        baitList.Add(10);

                    //GameController.instance.UpdateBetInstant(9, NumberPoints.nine);
                }
                if (numVal.Key == "11")
                {
                    NumberPoints.eleven = Convert.ToInt32(numVal.Value);
                    PlayPoints.text = (Convert.ToInt32(PlayPoints.text) + Convert.ToInt32(numVal.Value)).ToString();
                    if (!baitList.Contains(11))
                        baitList.Add(11);

                    //GameController.instance.UpdateBetInstant(9, NumberPoints.nine);
                }

            }

            GameController.instance.UpdateBetInstant(0, 0);
        }
        public void ClearParticularObject(GameObject obj)
        {

            string val = obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text;
            int number = Convert.ToInt32(val);
            if (number > 0 && (number - ChipValue >= 0))
            {


                switch (obj.name)
                {
                    case "0":
                        if (ChipValue <= NumberPoints.zero)
                            NumberPoints.zero -= ChipValue;
                        thZero -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thZero <= 0)
                        {
                            baitList.Remove(0);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thZero.ToString();
                        //baitList.Add(0);

                        break;
                    case "1":
                        if (ChipValue <= NumberPoints.one)
                            NumberPoints.one -= ChipValue;
                        thOne -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thOne <= 0)
                        {
                            baitList.Remove(1);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thOne.ToString();
                        //baitList.Add(1);
                        break;
                    case "2":
                        if (ChipValue <= NumberPoints.two)
                            NumberPoints.two -= ChipValue;
                        thTwo -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thTwo <= 0)
                        {
                            baitList.Remove(2);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thTwo.ToString();
                        //baitList.Add(2);
                        break;
                    case "3":
                        if (ChipValue <= NumberPoints.three)
                            NumberPoints.three -= ChipValue;
                        thThree -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thThree <= 0)
                        {
                            baitList.Remove(3);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thThree.ToString();
                        //baitList.Add(3);
                        break;
                    case "4":
                        if (ChipValue <= NumberPoints.four)
                            NumberPoints.four -= ChipValue;
                        thFour -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thFour <= 0)
                        {
                            baitList.Remove(4);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thFour.ToString();
                        //baitList.Add(4);
                        break;
                    case "5":
                        if (ChipValue <= NumberPoints.five)
                            NumberPoints.five -= ChipValue;
                        thFive -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thFive <= 0)
                        {
                            baitList.Remove(5);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thFive.ToString();
                        // baitList.Add(5);
                        break;
                    case "6":
                        if (ChipValue <= NumberPoints.six)
                            NumberPoints.six -= ChipValue;
                        thSix -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thSix <= 0)
                        {
                            baitList.Remove(6);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thSix.ToString();
                        // baitList.Add(6);
                        break;
                    case "7":
                        if (ChipValue <= NumberPoints.seven)
                            NumberPoints.seven -= ChipValue;
                        thSeven -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thSeven <= 0)
                        {
                            baitList.Remove(7);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thSeven.ToString();
                        // baitList.Add(7);
                        break;
                    case "8":
                        if (ChipValue <= NumberPoints.eight)
                            NumberPoints.eight -= ChipValue;
                        thEight -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thEight <= 0)
                        {
                            baitList.Remove(8);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thEight.ToString();
                        // baitList.Add(8);
                        break;
                    case "9":
                        if (ChipValue <= NumberPoints.nine)
                            NumberPoints.nine -= ChipValue;
                        thNine -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thNine <= 0)
                        {
                            baitList.Remove(9);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thNine.ToString();
                        // baitList.Add(9);
                        break;
                    case "10":
                        if (ChipValue <= NumberPoints.ten)
                            NumberPoints.ten -= ChipValue;
                        thTen -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thTen <= 0)
                        {
                            baitList.Remove(10);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thTen.ToString();
                        // baitList.Add(9);
                        break;
                    case "11":
                        if (ChipValue <= NumberPoints.eleven)
                            NumberPoints.eleven -= ChipValue;
                        thEleven -= ChipValue;
                        MainPlayValue -= ChipValue;
                        if (thEleven <= 0)
                        {
                            baitList.Remove(11);
                        }
                        obj.transform.GetChild(0).GetChild(0).gameObject.GetComponent<Text>().text = thEleven.ToString();
                        // baitList.Add(9);
                        break;
                }

                int toal = NumberPoints.zero + NumberPoints.one + NumberPoints.two + NumberPoints.three + NumberPoints.four + NumberPoints.five + NumberPoints.six + NumberPoints.seven + NumberPoints.eight + NumberPoints.nine + NumberPoints.ten + NumberPoints.eleven;
                MainPlayValue = toal;
                PlayPoints.text = MainPlayValue.ToString();
                MainPoints.text = (Convert.ToInt32(MainPoints.text) + ChipValue).ToString();

                GameController.instance.UpdateBetInstant(0, 0);
                //MainPlayValue = Convert.ToInt32(PlayPoints.text) - ChipValue;
                //PlayPoints.text = (MainPlayValue).ToString();
            }
        }
    }
}