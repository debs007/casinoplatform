using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Runtime.InteropServices;
using System;
using TMPro;
using System.Security.Cryptography.X509Certificates;
using SingleChance;
using UnityEngine.SceneManagement;
using bigsmall;

namespace TwelveCards
{
    public class ServerConnector : MonoBehaviour
    {
        public GameObject Loader;
        public static string _userId, _sessionId;
        string tempUid = "5", tempSid = "86d73rgiwni47mhfmujlejy7r2pavcqn";
        public GameObject Row;
        public RectTransform RowParent;
        public static ServerConnector serverConnector;
        public bool Testpurpose = false;
        public static bool Is_Jackpot = false;
        public static string UserData = "http://54.243.16.124:8000/gameinfo/", DataCollected = "http://54.243.16.124:8000/gameround/create/", ResultView = "http://54.243.16.124:8000/gameround/";

        //[DllImport("__Internal")]
        //private static extern string GetParamsFromUri();

        public GameObject Loading, LoginScreen, InvalidCred, GamingScreen;

        public InputField uId, Pass;
        public string uid, pass;

        public Dictionary<string, string> Users = new Dictionary<string, string>();
        public GameObject PlatformScreen;

        public RectTransform transferContent;
        public GameObject TObj;

        public InputField CreateAccount_email, CreateAccount_password;
        public Text register_msg;


        private void Awake()
        {
            serverConnector = this;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);
            if(Constants.IsLoggedIn)
            {
                Login();
            }
        }

        public void Login()
        {
            Loading.SetActive(true);
            InvalidCred.SetActive(false);
            StartCoroutine(LoginDirectly());
        }

        public void Register()
        {
            Loading.SetActive(true);
            // InvalidCred.SetActive(false);
            register_msg.text = "";
            StartCoroutine(RegisterDirectly());
        }

        IEnumerator RegisterDirectly()
        {
            WWWForm form = new();
            form.AddField("userName", CreateAccount_email.text);
            form.AddField("password", CreateAccount_password.text);
            form.AddField("stockist", "Jit");
            form.AddField("type", "agent");
            form.AddField("percent", "0");
            UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriRegister, form);
            // request.certificateHandler = new BypassCertificate();

            Loading.SetActive(true);
            yield return request.SendWebRequest();

            Loading.SetActive(false);
            bool login = false;
            Debug.LogError(request.downloadHandler.text);

            if (request.responseCode == 200)
            {

                UserResponse userResponse = JsonUtility.FromJson<UserResponse>(request.downloadHandler.text);
                SingleChance.UserResponse s_userResponse = JsonUtility.FromJson<SingleChance.UserResponse>(request.downloadHandler.text);
                bigsmall.UserResponse b_userReponse= JsonUtility.FromJson<bigsmall.UserResponse>(request.downloadHandler.text);

                if (userResponse.data.status == "blocked")
                {
                    InvalidCred.SetActive(true);
                    yield break;
                }
                login = true;

                Constants.userData = userResponse.data;

                //singlechance user data as well
                SingleChance.Constants.userData = s_userResponse.data;
                bigsmall.Constants.userData     = b_userReponse.data;

                GetPoints.getPoints.MainPoints.text = Constants.userData.balance.ToString();
                GetPoints.getPoints.MyName.text = "Welcome, " + Constants.userData.name;

                PlayerPrefs.SetString("name", CreateAccount_email.text);
                PlayerPrefs.SetString("pass", CreateAccount_password.text);
            }

            if (!login)
            {
                // InvalidCred.SetActive(true);

                register_msg.text = request.downloadHandler.text;
            }
            else
            {
                LoginScreen.SetActive(false);
                StartPlatform();


            }
        }
        IEnumerator LoginDirectly()
        {
            WWWForm form = new();
            form.AddField("userName", uId.text);
            form.AddField("password", Pass.text);
            UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriLogin, form);
            // request.certificateHandler = new BypassCertificate();

            Loading.SetActive(true);
            yield return request.SendWebRequest();

            Loading.SetActive(false);
            bool login = false;
            Debug.LogError(request.downloadHandler.text);
            // CopyResponse(request.downloadHandler.text);
            //foreach(KeyValuePair<string,string> usr in Users)
            //{
            //    if (uId.text == usr.Key && Pass.text == usr.Value)
            //    {
            //        LoginScreen.SetActive(false);
            //        GamingScreen.SetActive(true);
            //        login = true;
            //    }

            //}

            if (request.responseCode == 200)
            {






                UserResponse userResponse = JsonUtility.FromJson<UserResponse>(request.downloadHandler.text);
                SingleChance.UserResponse s_userResponse = JsonUtility.FromJson<SingleChance.UserResponse>(request.downloadHandler.text);
                bigsmall.UserResponse b_userReponse = JsonUtility.FromJson<bigsmall.UserResponse>(request.downloadHandler.text);

                if (userResponse.data.status == "blocked")
                {
                    InvalidCred.SetActive(true);
                    yield break;
                }
                login = true;

                Constants.userData = userResponse.data;
                //singlechance user data as well
                SingleChance.Constants.userData = s_userResponse.data;
                bigsmall.Constants.userData = b_userReponse.data;

                GetPoints.getPoints.MainPoints.text = Constants.userData.balance.ToString();
                GetPoints.getPoints.MyName.text = "Welcome, " + Constants.userData.name;

                PlayerPrefs.SetString("name", uId.text);
                PlayerPrefs.SetString("pass", Pass.text);
            }

            if (!login)
            {
                InvalidCred.SetActive(true);
            }
            else
            {
                LoginScreen.SetActive(false);
                StartPlatform();


            }


        }
        public InputField Username, Amount;
        public void TransferBalance()
        {
            if (int.Parse(Amount.text) <= 0)
            {
                NativeAlert.Show("Error", "Invalid Amount");
                return;
            }

            if (string.IsNullOrEmpty(Username.text.Trim(' ')))
            {
                NativeAlert.Show("Error", "Invalid username");
                return;
            }

            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);
            form.AddField("other", Username.text.Trim(' '));
            form.AddField("amount", Amount.text.Trim(' '));

            //  Debug.LogError($"Sending with : {Constants.userData.id} other : { Username.text.Trim(' ')} amount: { Amount.text.Trim(' ')}");

            StartCoroutine(BalanceTransfer(form));

        }

        public void GetTransferRecords()
        {
            foreach (Transform trn in transferContent)
            {
                Destroy(trn.gameObject);
            }

            StartCoroutine(GetTransfers());
        }

        IEnumerator GetTransfers()
        {
            UnityWebRequest request = UnityWebRequest.Get(Constants.baseUriApi + "getTransfers?id=" + Constants.userData.id);
            yield return request.SendWebRequest();

            Debug.LogError(request.downloadHandler.text);
            if (request.result == UnityWebRequest.Result.Success)
            {
                TransferResponse tr = JsonUtility.FromJson<TransferResponse>(request.downloadHandler.text);
                foreach (TransferDetails td in tr.response_data)
                {
                    GameObject tob = Instantiate(TObj);
                    tob.transform.parent = transferContent;
                    tob.transform.localScale = Vector3.one;

                    tob.GetComponent<TransferObjects>().SetTransferDetails(td.userName, td.amount, td.formatted_date);
                }
            }
        }

        IEnumerator BalanceTransfer(WWWForm form)
        {
            UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriBalanceTranfer, form);
            request.timeout = 10;

            yield return request.SendWebRequest();

            //  Debug.LogError(request.downloadHandler.text);

            if (request.result == UnityWebRequest.Result.Success)
            {
                NativeAlert.Show("Success", "Balance transfer successfull");

                Constants.userData.balance = Convert.ToInt32(request.downloadHandler.text);
                GetPoints.getPoints.MainPoints.text = request.downloadHandler.text;

                Username.text = "";
                Amount.text = "";
            }
            else
            {
                NativeAlert.Show("Failed", "Sorry! can not transfer balance");
                yield break;
            }
        }

        public void CopyResponse(string msg)
        {
            GUIUtility.systemCopyBuffer = msg;
        }

        public bool GameIsStarted = false;

        public void StartPlatform()
        {
            GameIsStarted = false;
            Constants.IsLoggedIn = true;
            PlatformScreen.SetActive(true);
        }

        public void StartSingleChance()
        {
            GameIsStarted = true;
            SceneManager.LoadSceneAsync(2);
        }

        public void StartBigSmall()
        {
            GameIsStarted = true;
            SceneManager.LoadSceneAsync(3);
        }
        public void Start12Card()
        {
            GameIsStarted = true;
            PlatformScreen.SetActive(false);
            GameController.instance.CheckSession();
            GamingScreen.SetActive(true);
            StartCoroutine(ScreenFlowHandler.screenFlow.SetOnlineStatus("yes"));

        }

        public IEnumerator GetUserDetails()
        {
            LoaderState("activate");

            WWWForm wWForm = new WWWForm();
            wWForm.AddField("username", tempUid);
            wWForm.AddField("session", tempSid);
            //UserDataFetcherParam udata = new UserDataFetcherParam();
            //udata.username = Convert.ToInt32(tempUid);
            //udata.session = tempSid;
            //string Jdata = JsonUtility.ToJson(udata);
            //Debug.Log(Jdata);
            //wWForm.AddBinaryData(Jdata)

            UnityWebRequest request = UnityWebRequest.Post(UserData, wWForm);
            Debug.Log("Sending request");
            yield return request.SendWebRequest();

            if (request.responseCode == 201)
            {
                LoaderState("no");
                Debug.Log("value fetched");
                Debug.Log(request.downloadHandler.text);
                Userdata userData = JsonUtility.FromJson<Userdata>(request.downloadHandler.text);

                Debug.Log("REsponse Stat: " + userData.ResponseStatus);
                _userId = userData.ResponseMessage.username.ToString();
                _sessionId = userData.ResponseMessage.session;

                GetPoints.getPoints.MainPoints.text = userData.ResponseMessage.token_granted.ToString();
                Debug.Log("Total points " + userData.ResponseMessage.token_granted);
                Debug.Log("username " + userData.ResponseMessage.session);

            }
            else
            {
                if (request.isHttpError)
                    Debug.Log("Bad request -> UserData()[]->Init Http error");
                else
                    Debug.Log("Bad request -> UserData()[]->unknown error" + request.error + "response_Code" + request.responseCode);
            }
        }

        public IEnumerator DataCollectedApi()
        {
            GetPoints.Is_Spinning = false;
            yield break;
            bool sent = false;
            //LoaderState("activate");


            Debug.Log("Sending data collected request");


            WWWForm wWForm = new WWWForm();
            wWForm.AddField("username", _userId);
            wWForm.AddField("session", _sessionId);
            wWForm.AddField("tokens_won", Convert.ToInt32(GetPoints.getPoints.tokensWon));
            wWForm.AddField("tokens_remaining", Convert.ToInt32(GetPoints.getPoints.MainPoints.text));
            wWForm.AddField("tokens_playing_for", Convert.ToInt32(GetPoints.getPoints.PlayPoints.text));
            wWForm.AddField("won_on_number", Convert.ToInt32(GetPoints.getPoints.won_on_number));
            wWForm.AddField("is_jackpot", Is_Jackpot ? 1 : 0);
            Is_Jackpot = false;
            UnityWebRequest request = UnityWebRequest.Post(DataCollected, wWForm);

            while (true)
            {
                yield return request.SendWebRequest();

                if (request.responseCode == 201)
                {
                    //LoaderState("activate");
                    Debug.Log("data sent successfully");
                    GetPoints.Is_Spinning = false;
                    sent = true;
                }
                else
                {
                    Debug.Log("The error: ");
                    if (request.isHttpError)
                        Debug.Log("Bad request -> DataCollected()[]->Init Http error");
                    else
                        Debug.Log("Bad request -> DataCollected()[]->unknown error");
                }
                if (sent)
                    break;
            }

        }

        public IEnumerator Result()
        {

            int count = 0;
            //ResultViewSendable sendable = new ResultViewSendable();
            //sendable.session = Convert.ToInt32(_sessionId);
            //string JData = JsonUtility.ToJson(sendable);
            UnityWebRequest request = UnityWebRequest.Get(ResultView + _sessionId + "/");
            Debug.Log("Sending result view request");
            yield return request.SendWebRequest();

            foreach (Transform trn in RowParent)
            {
                Destroy(trn.gameObject);
            }

            if (request.responseCode == 200)
            {
                Debug.Log("data sent successfully");

                ReslutViewData data = JsonUtility.FromJson<ReslutViewData>(request.downloadHandler.text);

                foreach (ReslutViewData_extended extended in data.ResponseMessage)
                {
                    count++;
                    GameObject obj = Instantiate(Row) as GameObject;
                    obj.GetComponent<RectTransform>().SetParent(RowParent);
                    obj.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);

                    obj.GetComponent<RectTransform>().GetChild(1).gameObject.GetComponent<Text>().text = extended.tokens_won.ToString();
                    obj.GetComponent<RectTransform>().GetChild(2).gameObject.GetComponent<Text>().text = extended.won_on_number != -90 ? extended.won_on_number.ToString() : "--";
                    obj.GetComponent<RectTransform>().GetChild(3).gameObject.GetComponent<Text>().text = extended.is_jackpot ? "Y" : "N";
                    obj.GetComponent<RectTransform>().GetChild(4).gameObject.GetComponent<Text>().text = extended.tokens_remaining.ToString();
                    obj.GetComponent<RectTransform>().GetChild(5).gameObject.GetComponent<Text>().text = extended.tokens_playing_for.ToString();

                }

                if (count < 6)
                {
                    for (int i = 0; i < 6 - count; i++)
                    {
                        GameObject obj = Instantiate(Row) as GameObject;
                        obj.GetComponent<RectTransform>().SetParent(RowParent);
                        obj.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
                    }


                }
            }

            else
            {
                Debug.Log("Result " + request.error);
                if (request.isHttpError)
                    Debug.Log("Bad request -> ResultView()[]->Init Http error");
                else
                    Debug.Log("Bad request -> ResultView()[]->unknown error");
                for (int i = 0; i < 6; i++)
                {
                    GameObject obj = Instantiate(Row) as GameObject;
                    obj.GetComponent<RectTransform>().SetParent(RowParent);
                    obj.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
                }
            }

        }

        public void GetInputTextUser(TextMeshProUGUI txt)
        {
            SetApis(txt.text, "User");
        }
        public void GetInputTextData(TextMeshProUGUI txt)
        {
            SetApis(txt.text, "Data");
        }
        public void GetInputTextResult(TextMeshProUGUI txt)
        {
            SetApis(txt.text, "Result");
        }

        public void SetApis(string url, string type)
        {
            switch (type)
            {
                case "User":
                    UserData = url;
                    break;
                case "Data":
                    DataCollected = url;
                    break;
                case "Result":
                    ResultView = url;
                    break;

            }
        }

        void LoaderState(string state)
        {
            if (state.Equals("activate"))
            {
                Loader.SetActive(true);
            }
            else
            {
                Loader.SetActive(false);
            }
        }

    }
    [System.Serializable]
    public class Userdata
    {
        public string ResponseStatus;
        public Userdata_extended ResponseMessage;
    }
    [System.Serializable]
    public class Userdata_extended
    {
        public int username;
        public string session;
        public int token_granted;
        public int probability;
    }
    [System.Serializable]
    public class UserDataFetcherParam
    {
        public int username;
        public string session;
    }
    [System.Serializable]
    public class DataCollected
    {
        public int username;
        public string session;
        public int tokens_won;
        public int tokens_remaining;
        public int tokens_playing_for;
        public int won_on_number;
        public int is_jackpot;
    }
    [System.Serializable]
    public class ReslutViewData
    {
        public string ResponseStatus;
        public List<ReslutViewData_extended> ResponseMessage;
    }
    [System.Serializable]
    public class ReslutViewData_extended
    {
        public int username;
        public string session;
        public int tokens_playing_for;
        public int tokens_won;
        public int tokens_remaining;
        public int won_on_number;
        public bool is_jackpot;
        public string insert_datetime;
    }
    [System.Serializable]
    public class ResultViewSendable
    {
        public int session;
    }

    [System.Serializable]
    public class TransferResponse
    {
        public TransferDetails[] response_data;
    }

    [System.Serializable]
    public class TransferDetails
    {
        public string userName;
        public string payerName;
        public string amount;
        public string formatted_date;
    }
}