using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TwelveCards
{
    public class Constants
    {
        //public static string baseUri = "https://khelo777.nebulaqueststudios.com/";
        //public static string baseUriApi = "https://khelo777.nebulaqueststudios.com/api/";
        // public static string baseUri = "https://ultrakhelojeetopc.nebulaqueststudios.com/";
        //public static string baseUriApi = "https://ultrakhelojeetopc.nebulaqueststudios.com/api/";
        public static string baseUri = "https://ultrakhelo.ultraarena.co.in/";
        public static string baseUriApi = "https://ultrakhelo.ultraarena.co.in/api/";
        public static string baseUriLogin = baseUriApi + "login";
        public static string baseUriRegister = baseUriApi + "register";
        public static string baseUriDeatils = baseUriApi + "details";
        public static string baseUriUpdate = baseUriApi + "updateBet";
        public static string baseUriWinnumber = baseUriApi + "getWin";
        public static string getmyBets = baseUriApi + "getMyBets";
        public static string setWin = baseUriApi + "setWinValue";
        public static string baseUriSession = baseUri + "getNewSession";
        public static string baseUriStatus = baseUriApi + "setStatus";
        public static string baseUriBalanceTranfer = baseUriApi + "transferBalance";
        public static bool IsLoggedIn =false;
        public static UserData userData;
        public static SessionResponse sessionData;
        public static string[] cards = new string[] { "J heart", "J spades", "J diamond", "J club", "K heart", "K spades", "K diamond", "K club", "Q heart", "Q spades", "Q diamond", "Q club" };
        public static string[] cardsWithSymbol = new string[] { "J <color=red>❤</color>", "J ♠", "J <color=red>♦</color>", "J ♣", "K <color=red>❤</color>", "K ♠", "K <color=red>♦</color>", "K ♣", "Q <color=red>❤</color>", "Q ♠", "Q <color=red>♦</color>", "Q ♣" };
        public static string[] symbol = new string[] { "heart", "spades", "diamond", "club", "heart", "spades", "diamond", "club", "heart", "spades", "diamond", "club" };
        public static List<MyBetData> tickets = new List<MyBetData>();
    }
    [System.Serializable]
    public class UserResponse
    {
        public UserData data;
    }

    [System.Serializable]
    public class UserData
    {
        public string id;
        public int balance;
        public string token;
        public string name;
        public string stockist;
        public string play;
        public string totalPlayPoints;
        public string winPoint;
        public string status;
        public int commisionReceived;
        public string whatsapp;
        public string qr;
        public string qrImage;
    }
    [System.Serializable]
    public class SessionResponse
    {
        public int diff;
        public string session;
        public string sessionNum;
        public Prevbets[] bets;
        public int totalTime;
        public int lastChance;
        public int minLimit;
        public int fSpeed;
        public int sSpeed;
        public float stoper;
    }
    [System.Serializable]
    public class Prevbets
    {
        public string number;
        public string times;
        public string time;
    }
    [System.Serializable]
    public class GetWinNumber
    {
        public string number;
        public int times;
        public string status;
        public string time;
    }

    [System.Serializable]
    public class MyBets
    {
        public MyBetData[] allBets;
        public string totalPlayPoint;
        public string winPoint;
        public string date;
    }

    [System.Serializable]
    public class MyBetData
    {
        public string amount;
        public string bets;
        public string created_at;
        public string won;
        public string sessionId;
        public string sessionNum;

        public string time;
        public string win;
        public int number;
        public int beforeBalance;
        public int afterBalance;
    }

    [System.Serializable]
    public class Stores
    {
        public List<MyBetData> mybets = new List<MyBetData>();
    }
}