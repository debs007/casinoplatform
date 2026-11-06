using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace bigsmall
{
    public class Constants 
    {
        public static string baseUri = "https://bigsmall.ultraarena.co.in/";
        public static string baseUriApi = "https://bigsmall.ultraarena.co.in/api/";
        public static string baseUriLogin = baseUriApi + "login";
        public static string baseUriUpdate = baseUriApi + "updateBet";
        public static string baseUriWinnumber = baseUriApi + "getWin";
        public static string getmyBets = baseUriApi + "getMyBets";
        public static string setWin = baseUriApi + "setWinValue";
        public static string baseUriSession = baseUri + "getNewSession";
        public static string baseUriStatus = baseUriApi + "setStatus";
        public static UserData userData;
        public static SessionResponse sessionData;
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
        public string dice;     // Big Small: dice faces, e.g. "2,4"
    }
    [System.Serializable]
    public class GetWinNumber
    {
        public string number;
        public int times;
        public string status;
        public string dice;     // Big Small: dice faces, e.g. "2,4"
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

        public string time;
        public string win;
        public int number;
        public int beforeBalance;
        public int afterBalance;
    }
}
