using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace bigsmall
{
    /// <summary>
    /// Big Small: the whole game in one script (player data and server addresses come from Constants).
    ///
    ///   Small = 2-6 (pays x2)   7 = jackpot (pays x5)   Big = 8-12 (pays x2)
    ///
    /// Round: bet -> "no more bets" at 5s -> result from the server -> CasinoDiceRoller.Roll(dice1, dice2)
    /// -> your dice-stop code calls OnDiceRollFinished(number) -> winning area animation -> win -> next round.
    /// Buttons are hooked up from the Inspector fields; no OnClick setup is needed.
    /// </summary>
    [DisallowMultipleComponent]
    public class BigSmallGame : MonoBehaviour
    {
        public enum Area { Small = 0, Seven = 1, Big = 2 }

        /// <summary>Winning Small / Big returns its bet x2, 7 returns x5. Must match SetBet::PAYOUTS on the server.</summary>
        public const int SmallBigPayout = 2;
        public const int JackpotPayout = 5;

        /// <summary>A text that can be a UI Text or a TextMeshPro text: fill in whichever you use.</summary>
        [Serializable]
        public class TextRef
        {
            public Text text;
            public TMP_Text tmp;

            public void Set(string value)
            {
                if (text != null) text.text = value;
                if (tmp != null) tmp.text = value;
            }
        }

        [Serializable]
        public class Chip
        {
            public int value = 10;
            public Button button;
            [Tooltip("Optional: shown while this chip is selected")]
            public GameObject selectedMark;
        }

        [Header("Chips (the first one is selected at start)")]
        public Chip[] chips = new Chip[0];

        [Header("Bet areas")]
        public Button smallButton;
        public Button sevenButton;
        public Button bigButton;
        [Tooltip("Chips on Small not sent yet. Becomes empty when the bet is confirmed.")]
        public TextRef smallBetText = new TextRef();
        [Tooltip("Chips on 7 not sent yet. Becomes empty when the bet is confirmed.")]
        public TextRef sevenBetText = new TextRef();
        [Tooltip("Chips on Big not sent yet. Becomes empty when the bet is confirmed.")]
        public TextRef bigBetText = new TextRef();

        public GameObject DiceArea;
        public GameObject MessageArea;

        [Header("Bet")]
        public Button betButton;
        [Tooltip("Optional: removes the chips not sent yet")]
        public Button clearButton;

        [Header("Info")]
        public TextRef balanceText = new TextRef();
        public TextRef nameText = new TextRef();

        [Tooltip("Total of the bets confirmed this round. Reset when the next round starts.")]
        public TextRef totalPlayText = new TextRef();
        public TextRef timerText = new TextRef();
        [Tooltip("Optional: round number")]
        public TextRef sessionText = new TextRef();
        [Tooltip("Optional: short messages (Bet placed, No more bets, SMALL wins ...)")]
        public TextRef messageText = new TextRef();
        public float messageSeconds = 2.5f;

        [Header("Round timing")]
        [Tooltip("Betting stops with this many seconds left on the clock")]
        public int noMoreBetsAt = 5;
        [Tooltip("Join a running round only if at least this many seconds are left, otherwise wait for the next")]
        public int minSecondsToJoin = 10;
        [Tooltip("If OnDiceRollFinished is never called, carry on after this many seconds")]
        public float diceTimeout = 20f;

        [Header("Dice")]
        public CasinoDiceRoller diceRoller;

        [Header("Winning area animations (created, then destroyed after Animation Seconds)")]
        public GameObject smallWinPrefab;
        public GameObject bigWinPrefab;
        public GameObject jackpotPrefab;
        [Tooltip("Parent for the animations, e.g. your Canvas. Empty = scene root.")]
        public Transform animationParent;
        public float animationSeconds = 3f;

        [Header("Win")]
        [Tooltip("Shown when the player wins")]
        public GameObject winPanel;
        public TextRef winAmountText = new TextRef();
        public float winSeconds = 3f;

        [Header("Popups")]
        public GameObject lowBalancePopup;
        [Tooltip("Hide the low balance popup after this many seconds (0 = until CloseLowBalancePopup is called)")]
        public float lowBalanceSeconds = 2f;
        [Tooltip("Optional: shown while waiting for the next round")]
        public GameObject waitingPanel;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip placeYourBetsClip;
        public AudioClip noMoreBetsClip;
        public AudioClip chipClip;
        public AudioClip winClip;

        enum Phase { Waiting, Betting, Closed, Rolling, Result }

        Phase phase = Phase.Waiting;
        readonly int[] pending = new int[3];     // chips placed but not sent, per Area
        readonly int[] confirmed = new int[3];   // bets the server accepted this round, per Area
        int selectedChip;
        int serverBalance;
        int roundPlay;                           // total played this round (from the server)
        bool betInFlight;
        int roundId;                             // so a late reply can't touch a newer round
        string session;
        float roundEndsAt;                       // realtimeSinceStartup when the clock reaches 0
        int resultNumber;
        bool diceFinished;
        Coroutine messageRoutine;
        Coroutine lowBalanceRoutine;

        public static BigSmallGame Instance { get; private set; }

        /// <summary>This round's result (2-12) and its dice faces from the server; 0 until known.</summary>
        public int ResultNumber { get { return resultNumber; } }
        public int ResultDice1 { get; private set; }
        public int ResultDice2 { get; private set; }

        /// <summary>Raised once the dice have stopped: result number, dice 1, dice 2 (server faces). ResultHistory listens to it.</summary>
        public event Action<int, int, int> DiceStopped;

        /// <summary>Last results (number + dice faces) sent with the round, for a history UI.</summary>
        public Prevbets[] LastResults { get { return Constants.sessionData != null ? Constants.sessionData.bets : null; } }

        public int SecondsLeft { get { return Mathf.Max(0, Mathf.CeilToInt(roundEndsAt - Time.realtimeSinceStartup)); } }

        int PendingTotal { get { return pending[0] + pending[1] + pending[2]; } }
        int AvailableBalance { get { return serverBalance - PendingTotal; } }
        bool CanBet { get { return phase == Phase.Betting && !betInFlight; } }

        // ------------------------------------------------------------------ setup

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            foreach (Chip chip in chips)
            {
                if (chip == null || chip.button == null) continue;
                int value = chip.value;
                chip.button.onClick.AddListener(() => SelectChip(value));
            }
            if (smallButton != null) smallButton.onClick.AddListener(BetOnSmall);
            if (sevenButton != null) sevenButton.onClick.AddListener(BetOnSeven);
            if (bigButton != null) bigButton.onClick.AddListener(BetOnBig);
            if (betButton != null) betButton.onClick.AddListener(PlaceBet);
            if (clearButton != null) clearButton.onClick.AddListener(ClearBets);

            if (chips.Length > 0 && chips[0] != null) SelectChip(chips[0].value);

            if (winPanel != null) winPanel.SetActive(false);
            if (lowBalancePopup != null) lowBalancePopup.SetActive(false);
            RefreshTexts();
            nameText.text.text = $"Welcome, {Constants.userData.name}";
            StartCoroutine(GameLoop());
        }

        void Update()
        {
            RefreshButtons();
            RefreshTimer();
        }

        //--------------Screen Flow----------------------------------------//

        public void GotoMenu()
        {
            SceneManager.LoadSceneAsync(1);
        }

        // ------------------------------------------------------------------ the round

        IEnumerator GameLoop()
        {
            // Constants.userData is filled by your login
            if (Constants.userData == null)
            {
                ShowMessage("Waiting for login...");
                yield return new WaitUntil(() => Constants.userData != null);
            }
            serverBalance = Constants.userData.balance;
            RefreshTexts();
            // marks the player online: the server's generateAutoBet cron finishes rounds by itself
            // while nobody is online, which would close betting for everyone mid-round
            StartCoroutine(SendStatus("yes"));
            yield return RefreshBalance(0);

            while (true)
            {
                yield return WaitForRound();    // (12) next round, everything enabled
                yield return Betting();         // (1)-(4)
                yield return NoMoreBets();      // (5)
                yield return FetchResult();     // (6)
                yield return RollDice();        // (7), (8)
                yield return ShowResult();      // (9)-(11)
            }
        }

        IEnumerator WaitForRound()
        {
            phase = Phase.Waiting;
            if (waitingPanel != null) waitingPanel.SetActive(true);

            string finished = session;
            string settled = null;
            while (true)
            {
                SessionResponse s = null;
                yield return GetSession(r => s = r);
                if (s != null && s.session != finished)
                {
                    if (s.diff >= minSecondsToJoin)
                    {
                        StartRound(s);
                        yield break;
                    }
                    // a round we didn't join has ended: ask the server to finish it, otherwise the
                    // next round only starts much later when nobody else is playing
                    if (s.diff <= 0 && s.session != settled)
                    {
                        bool done = false;
                        yield return RequestResult(s.session, ok => done = ok);
                        if (done) settled = s.session;
                    }
                }
                yield return new WaitForSecondsRealtime(2f);
            }
        }

        IEnumerator RequestResult(string forSession, Action<bool> done)
        {
            WWWForm form = new WWWForm();
            form.AddField("session", forSession);
            using (UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriWinnumber, form))
            {
                request.timeout = 30;
                yield return request.SendWebRequest();
                done(request.responseCode == 200);
            }
        }

        void StartRound(SessionResponse s)
        {
            Constants.sessionData = s;
            session = s.session;
            roundId++;
            roundEndsAt = Time.realtimeSinceStartup + s.diff;

            for (int i = 0; i < 3; i++) { pending[i] = 0; confirmed[i] = 0; }
            roundPlay = 0;
            resultNumber = 0;
            ResultDice1 = 0;
            ResultDice2 = 0;

            sessionText.Set(s.sessionNum);
            if (waitingPanel != null) waitingPanel.SetActive(false);
            phase = Phase.Betting;
            RefreshTexts();
            Play(placeYourBetsClip);
        }

        IEnumerator Betting()
        {
            while (SecondsLeft > noMoreBetsAt)
            {
                yield return null;
            }
        }

        IEnumerator NoMoreBets()
        {
            phase = Phase.Closed;
            Play(noMoreBetsClip);

            if (PendingTotal > 0 && !betInFlight)
            {
                ClearPending();
                ShowMessage("No more bets - chips not sent were removed");
            }
            else
            {
                ShowMessage("No more bets");
            }

            // wait for the clock, and for a bet reply that may still be on its way
            while (SecondsLeft > 0 || betInFlight)
            {
                yield return null;
            }
        }

        IEnumerator FetchResult()
        {
            while (true)
            {
                WWWForm form = new WWWForm();
                form.AddField("session", session);
                using (UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriWinnumber, form))
                {
                    request.timeout = 30;
                    yield return request.SendWebRequest();

                    if (request.responseCode == 200)
                    {
                        GetWinNumber result = JsonUtility.FromJson<GetWinNumber>(request.downloadHandler.text);
                        int number;
                        if (result != null && int.TryParse(result.number, out number) && number >= 2 && number <= 12)
                        {
                            resultNumber = number;
                            int d1, d2;
                            ParseDice(result.dice, out d1, out d2);
                            ResultDice1 = d1;
                            ResultDice2 = d2;
                            yield break;
                        }
                    }
                    Debug.Log("getWin [" + request.responseCode + "] " + request.downloadHandler.text + " - asking again");
                }
                // 202 "Result not ready" or a network error: ask again shortly
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        IEnumerator RollDice()
        {
            phase = Phase.Rolling;
            diceFinished = false;

            if (diceRoller != null)
            {
                // the server's two faces, so every player sees the same dice (total only if they're missing)
                if (ResultDice1 >= 1 && ResultDice1 <= 6 && ResultDice2 >= 1 && ResultDice2 <= 6 && ResultDice1 + ResultDice2 == resultNumber)
                    diceRoller.Roll(ResultDice1, ResultDice2);
                else
                    diceRoller.Roll(resultNumber);
            }
            else
            {
                Debug.LogError("BigSmallGame: Dice Roller is not assigned. Result was " + resultNumber);
                diceFinished = true;
            }

            float waited = 0f;
            while (!diceFinished && waited < diceTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!diceFinished)
            {
                Debug.LogWarning("BigSmallGame: OnDiceRollFinished was not called within " + diceTimeout + "s, showing the result anyway");
            }

            if (DiceStopped != null)
            {
                // a listener's error must not stop the game loop
                try { DiceStopped(resultNumber, ResultDice1, ResultDice2); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        /// <summary>(8) Call this when the dice have stopped, with the number they show.</summary>
        public void OnDiceRollFinished(int number)
        {
            if (phase != Phase.Rolling)
            {
                return;
            }
            if (number != resultNumber)
            {
                Debug.LogWarning("BigSmallGame: dice show " + number + " but the server result is " + resultNumber + ". The server result is used.");
            }
            diceFinished = true;
        }

        IEnumerator ShowResult()
        {
            phase = Phase.Result;
            Area winner = AreaOf(resultNumber);

            // (9), (11) which area won
            ShowMessage(winner == Area.Seven ? "7 JACKPOT" : (winner == Area.Small ? "SMALL" : "BIG") + " (" + resultNumber + ")");
            GameObject prefab = winner == Area.Small ? smallWinPrefab : (winner == Area.Big ? bigWinPrefab : jackpotPrefab);
            if (prefab != null)
            {
                GameObject anim = animationParent != null ? Instantiate(prefab, animationParent, false) : Instantiate(prefab);
                Destroy(anim, animationSeconds);
            }
            yield return new WaitForSecondsRealtime(animationSeconds);

            // (10) Small / Big x2, 7 x5 of what was bet on the winning area
            int win = confirmed[(int)winner] * (winner == Area.Seven ? JackpotPayout : SmallBigPayout);
            if (win > 0)
            {
                winAmountText.Set(win.ToString());
                if (winPanel != null) winPanel.SetActive(true);
                DiceArea.SetActive(false);
                Play(winClip);
                yield return new WaitForSecondsRealtime(winSeconds);
                if (winPanel != null) winPanel.SetActive(false);
                DiceArea.SetActive(true);
            }

            // the server already paid out; show the new balance only now so it doesn't spoil the dice
            yield return RefreshBalance(win);
        }

        // ------------------------------------------------------------------ buttons

        /// <summary>Chip button: selects the chip value used by the next taps.</summary>
        public void SelectChip(int value)
        {
            selectedChip = value;
            foreach (Chip chip in chips)
            {
                if (chip != null && chip.selectedMark != null) chip.selectedMark.SetActive(chip.value == value);
            }
        }

        public void BetOnSmall() { AddChip(Area.Small); }
        public void BetOnSeven() { AddChip(Area.Seven); }
        public void BetOnBig() { AddChip(Area.Big); }

        void AddChip(Area area)
        {
            if (!CanBet)
            {
                return;
            }
            if (selectedChip <= 0)
            {
                ShowMessage("Select a chip first");
                return;
            }
            if (AvailableBalance < selectedChip)
            {
                ShowLowBalance();    // (13)
                return;
            }
            pending[(int)area] += selectedChip;
            Play(chipClip);
            RefreshTexts();
        }

        /// <summary>Removes the chips not sent yet.</summary>
        public void ClearBets()
        {
            if (!CanBet)
            {
                return;
            }
            ClearPending();
        }

        /// <summary>(2) Bet button: sends all chips on the table in one request.</summary>
        public void PlaceBet()
        {
            if (!CanBet || PendingTotal <= 0)
            {
                return;
            }
            int[] slip = (int[])pending.Clone();
            int[] totals = new int[3];
            for (int i = 0; i < 3; i++)
            {
                // the server takes this round's totals per area (already confirmed + new chips)
                totals[i] = confirmed[i] + slip[i];
            }
            betInFlight = true;
            StartCoroutine(SendBet(totals, roundId, session));
        }

        IEnumerator SendBet(int[] totals, int sentRound, string sentSession)
        {
            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);
            form.AddField("session", sentSession);
            form.AddField("small", totals[(int)Area.Small]);
            form.AddField("seven", totals[(int)Area.Seven]);
            form.AddField("big", totals[(int)Area.Big]);
            form.AddField("amount", totals[0] + totals[1] + totals[2]);

            string body = "";
            long code = 0;
            using (UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriUpdate, form))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                code = request.responseCode;
                body = request.downloadHandler != null ? request.downloadHandler.text.Trim() : "";
                Debug.Log("updateBet [" + code + "] " + body);
            }

            betInFlight = false;
            if (sentRound != roundId)
            {
                yield break;     // that round is over
            }

            int balance, play;
            if (code == 200 && TryParseBetReply(body, out balance, out play))
            {
                // (3) confirmed: area texts become empty, (4) round total updated
                for (int i = 0; i < 3; i++) { confirmed[i] = totals[i]; pending[i] = 0; }
                SetBalance(balance);
                roundPlay = play;
                ShowMessage("Bet placed");
            }
            else if (body == "low_balance")
            {
                ClearPending();
                ShowLowBalance();
                yield return RefreshBalance(0);
            }
            else if (body == "closed")
            {
                ClearPending();
                ShowMessage("Betting closed");
            }
            else
            {
                // network/server error: keep the chips so the player can press Bet again (safe to resend)
                if (phase != Phase.Betting) ClearPending();
                ShowMessage("Bet not placed, please try again");
            }
            RefreshTexts();
        }

        /// <summary>Close button of the low balance popup.</summary>
        public void CloseLowBalancePopup()
        {
            if (lowBalancePopup != null) lowBalancePopup.SetActive(false);
        }

        // ------------------------------------------------------------------ helpers

        public static Area AreaOf(int number)
        {
            if (number <= 6) return Area.Small;
            if (number == 7) return Area.Seven;
            return Area.Big;
        }

        void ClearPending()
        {
            for (int i = 0; i < 3; i++) pending[i] = 0;
            RefreshTexts();
        }

        void SetBalance(int balance)
        {
            serverBalance = balance;
            if (Constants.userData != null) Constants.userData.balance = balance;

            RefreshTexts();
        }

        void RefreshTexts()
        {
            smallBetText.Set(pending[(int)Area.Small] > 0 ? pending[(int)Area.Small].ToString() : "");
            sevenBetText.Set(pending[(int)Area.Seven] > 0 ? pending[(int)Area.Seven].ToString() : "");
            bigBetText.Set(pending[(int)Area.Big] > 0 ? pending[(int)Area.Big].ToString() : "");
            balanceText.Set(AvailableBalance.ToString());
            totalPlayText.Set(roundPlay.ToString());
        }

        void RefreshButtons()
        {
            bool open = CanBet;
            foreach (Chip chip in chips)
            {
                if (chip != null && chip.button != null) chip.button.interactable = open;
            }
            if (smallButton != null) smallButton.interactable = open;
            if (sevenButton != null) sevenButton.interactable = open;
            if (bigButton != null) bigButton.interactable = open;
            if (betButton != null) betButton.interactable = open && PendingTotal > 0;
            if (clearButton != null) clearButton.interactable = open && PendingTotal > 0;
        }

        void RefreshTimer()
        {
            if (phase == Phase.Waiting)
            {
                timerText.Set("--:--");
                return;
            }
            int left = (phase == Phase.Betting || phase == Phase.Closed) ? SecondsLeft : 0;
            timerText.Set((left % 60).ToString("00"));
        }

        void ShowLowBalance()
        {
            if (lowBalancePopup == null)
            {
                ShowMessage("Low balance");
                return;
            }
            lowBalancePopup.SetActive(true);
            if (lowBalanceRoutine != null) StopCoroutine(lowBalanceRoutine);
            if (lowBalanceSeconds > 0f) lowBalanceRoutine = StartCoroutine(HideAfter(lowBalancePopup, lowBalanceSeconds));
        }

        IEnumerator HideAfter(GameObject go, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (go != null) go.SetActive(false);
        }

        void ShowMessage(string message)
        {
            messageText.Set(message);
            MessageArea.SetActive(true);
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(ClearMessageAfter(messageSeconds));
        }

        IEnumerator ClearMessageAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            messageText.Set("");
            MessageArea.SetActive(false);
        }

        void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        IEnumerator GetSession(Action<SessionResponse> done)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(Constants.baseUriSession))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                SessionResponse s = null;
                if (request.responseCode == 200)
                {
                    s = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);
                    if (s != null && string.IsNullOrEmpty(s.session)) s = null;
                }
                done(s);
            }
        }

        /// <summary>Reads the balance from the server (it has already paid any win). If that fails, adds the expected win.</summary>
        IEnumerator RefreshBalance(int fallbackWin)
        {
            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);
            using (UnityWebRequest request = UnityWebRequest.Post(Constants.setWin, form))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                int balance;
                if (request.responseCode == 200 && int.TryParse(request.downloadHandler.text.Trim(), out balance))
                {
                    SetBalance(balance);
                }
                else if (fallbackWin > 0)
                {
                    SetBalance(serverBalance + fallbackWin);
                }
            }
        }

        static bool TryParseBetReply(string body, out int balance, out int play)
        {
            balance = 0;
            play = 0;
            string[] parts = body.Split('|');
            return parts.Length >= 2 && int.TryParse(parts[0].Trim(), out balance) && int.TryParse(parts[1].Trim(), out play);
        }

        static bool ParseDice(string dice, out int dice1, out int dice2)
        {
            dice1 = 0;
            dice2 = 0;
            if (string.IsNullOrEmpty(dice)) return false;
            string[] parts = dice.Split(',');
            return parts.Length == 2 && int.TryParse(parts[0].Trim(), out dice1) && int.TryParse(parts[1].Trim(), out dice2);
        }

        IEnumerator SendStatus(string online)
        {
            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);
            form.AddField("data", online);
            using (UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriStatus, form))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (Constants.userData != null) StartCoroutine(SendStatus(paused ? "no" : "yes"));
        }

        void OnApplicationQuit()
        {
            if (Constants.userData == null) return;
            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);
            form.AddField("data", "no");
            UnityWebRequest.Post(Constants.baseUriStatus, form).SendWebRequest();   // not waited for: the app is closing
        }

        // after a minimise the local clock can drift: re-read the time left from the server
        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && session != null && (phase == Phase.Betting || phase == Phase.Closed))
            {
                StartCoroutine(Resync());
            }
        }

        IEnumerator Resync()
        {
            SessionResponse s = null;
            yield return GetSession(r => s = r);
            if (s == null)
            {
                yield break;
            }
            // same round: correct the clock; the round is over: go straight to its result
            roundEndsAt = Time.realtimeSinceStartup + (s.session == session ? s.diff : 0);
        }
    }
}