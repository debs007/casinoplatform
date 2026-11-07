using System;

namespace roulette
{
    /// <summary>Server addresses and the logged-in player, shared by the roulette scripts.</summary>
    public static class Constants
    {
        // The roulette backend (its own folder / domain, not Big Small's)
        public static string baseUri = "https://roulette.nebulaqueststudios.com/";
        public static string baseUriApi = baseUri + "api/";

        public static string baseUriLogin = baseUriApi + "login";
        public static string baseUriUpdate = baseUriApi + "updateBet";
        public static string baseUriWinnumber = baseUriApi + "getWin";
        public static string setWin = baseUriApi + "setWinValue";          // balance (without "laxmi")
        public static string getmyBets = baseUriApi + "getMyBets";
        public static string recentResults = baseUriApi + "recentResults";
        public static string todayResults = baseUriApi + "todayResults";
        public static string baseUriStatus = baseUriApi + "setStatus";
        public static string baseUriSession = baseUri + "getNewSession";

        public static string name, pass;

        /// <summary>Filled by the login (RouletteLogin or your own login scene).</summary>
        public static UserData userData;
        /// <summary>The round being played (set when a round starts).</summary>
        public static SessionResponse sessionData;
    }

    [Serializable]
    public class UserResponse
    {
        public UserData data;
    }

    [Serializable]
    public class UserData
    {
        public string id;
        public int balance;
        public string token;
        public string name;
        public string stockist;
        public string status;
    }

    [Serializable]
    public class SessionResponse
    {
        public int diff;                // seconds of betting left (negative once the round is over)
        public string session;
        public string sessionNum;       // round number shown to players
        public string status;           // "pending" while betting is open, "cleared" once the result is out
        public ResultRow[] bets;        // latest results, newest first
        public int totalTime;           // betting seconds per round
        public int nextRoundAfter;      // seconds between the end of betting and the next round
        public int lastChance;
        public int minLimit;
    }

    /// <summary>One past result.</summary>
    [Serializable]
    public class ResultRow
    {
        public string sessionId;
        public string sessionNum;
        public int number;              // 0-36
        public string color;            // "red", "black" or "green"
        public string time;             // "14:05:30"
    }

    [Serializable]
    public class WinResponse
    {
        public int number;
        public string color;
        public int times;
        public string status;
        public string time;
    }

    [Serializable]
    public class RecentResults
    {
        public ResultRow[] results;
    }

    [Serializable]
    public class TodayResults
    {
        public string date;
        public int red;                 // how many times each colour came up today
        public int black;
        public int green;
        public ResultRow[] results;
    }

    [Serializable]
    public class MyBets
    {
        public MyBetRow[] allBets;
        public int totalPlayPoint;
        public int winPoint;
        public string date;
        public string playerName;
    }

    [Serializable]
    public class MyBetRow
    {
        public string session;
        public string sessionNum;
        public int amount;              // total bet that round
        public string bets;             // "17:50,10-11:20,red:100"
        public BetPosition[] positions; // the same, with readable names
        public int number;              // result, -1 while pending
        public string won;
        public string color;
        public int win;                 // paid back (stake included), 0 if lost
        public string status;           // "won", "lost" or "pending"
        public string time;
    }

    [Serializable]
    public class BetPosition
    {
        public string bet;              // "17", "10-11", "Red", "1st 12" ...
        public int amount;
    }
}
