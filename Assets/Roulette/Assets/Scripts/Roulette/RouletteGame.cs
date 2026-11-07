using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Commands;
using Components;
using Controllers;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using ViewModel;

namespace roulette
{
    /// <summary>
    /// Casino roulette driven by the server: same rounds, same clock and same result for every player.
    ///
    /// Round: chips on the table -> Bet button sends them -> "no more bets" at 5s -> at 0 every phone
    /// spins the wheel -> the ball lands on the server's number -> win shown (the server already paid)
    /// -> chips cleared -> next round.
    ///
    /// Uses the project's own table, chips, wheel and displays (CharacterTable, GameRoullete ...):
    /// nothing is drawn by this script except the timer / round / message texts.
    /// The money shown is always the server balance minus the chips not sent yet.
    /// </summary>
    [DisallowMultipleComponent]
    public class RouletteGame : MonoBehaviour
    {
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

        [Header("Project assets (the same ones the table uses)")]
        public CharacterTable characterTable;
        public GameRoullete gameRoullete;
        public GameCmdFactory gameCmdFactory;
        [Tooltip("Optional: the Magnet object. Sweeps the chips off after each round.")]
        public MagnetDestroyerDisplay magnet;
        [Tooltip("Optional: the last numbers strip (LastNumberAnchor). Filled with the server's history.")]
        public LastTableNumberDisplay lastNumbers;

        [Header("Info texts")]
        public TextRef timerText = new TextRef();
        [Tooltip("Optional: round number")]
        public TextRef sessionText = new TextRef();
        [Tooltip("Optional: short messages (Bet placed, No more bets, 17 BLACK ...)")]
        public GameObject MessageArea;
        public TextRef messageText = new TextRef();
        public float messageSeconds = 2.5f;

        [Header("Waiting / popups")]
        [Tooltip("Optional: shown while waiting for the next round")]
        public GameObject waitingPanel;
        [Tooltip("Optional: \"Next round in 12s\"")]
        public TextRef waitingText = new TextRef();
        public GameObject lowBalancePopup;
        [Tooltip("Hide the low balance popup after this many seconds (0 = until CloseLowBalancePopup)")]
        public float lowBalanceSeconds = 2f;

        [Header("Round timing")]
        [Tooltip("Betting stops with this many seconds left on the clock")]
        public int noMoreBetsAt = 5;
        [Tooltip("Join a running round only if at least this many seconds are left, otherwise wait for the next")]
        public int minSecondsToJoin = 10;
        [Tooltip("Give up waiting for the result after this many seconds")]
        public float resultTimeout = 40f;

        [Header("Audio (optional)")]
        public AudioSource audioSource;
        public AudioClip placeYourBetsClip;
        public AudioClip noMoreBetsClip;

        public static RouletteGame Instance { get; private set; }

        /// <summary>The number of the round being shown, -1 until the server sends it.</summary>
        public int ResultNumber { get; private set; } = -1;
        public bool BettingOpen { get { return phase == Phase.Betting; } }
        public int SecondsLeft { get { return Mathf.Max(0, Mathf.CeilToInt(roundEndsAt - Time.realtimeSinceStartup)); } }
        /// <summary>Raised when the ball has landed (the number).</summary>
        public event Action<int> BallLanded;

        enum Phase { Waiting, Betting, Closed, Spinning, Result }

        class PlacedChip
        {
            public string key;          // numbers the spot covers, "10-11"
            public int value;           // chip value when it was placed
            public Chip chip;
            public ButtonTable button;
            public bool confirmed;      // accepted by the server: can't be taken back
        }

        class RepeatChip
        {
            public Chip chip;
            public ButtonTable button;
        }

        Phase phase = Phase.Waiting;
        readonly Dictionary<ChipGame, PlacedChip> placed = new Dictionary<ChipGame, PlacedChip>();
        readonly HashSet<ChipGame> removing = new HashSet<ChipGame>();   // destroyed at the end of this frame
        readonly Dictionary<string, int> confirmedTotals = new Dictionary<string, int>();   // this round, accepted
        readonly List<RepeatChip> lastRound = new List<RepeatChip>();
        int serverBalance;
        bool betInFlight;
        bool cleaning;
        bool resultFailed;
        int roundId;                    // a late reply can't touch a newer round
        string session;
        string finishedSession;
        readonly HashSet<string> stripSessions = new HashSet<string>();   // rounds on the last numbers strip
        float roundEndsAt;
        Coroutine messageRoutine;
        Coroutine lowBalanceRoutine;

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
            if (lowBalancePopup != null) lowBalancePopup.SetActive(false);
            if (waitingPanel != null) waitingPanel.SetActive(true);
            StartCoroutine(GameLoop());
        }

        void LateUpdate()
        {
            if (characterTable == null) return;
            TrackChips();

            // the server balance is the truth; chips not sent yet are only taken off the display
            int pending = PendingValue();
            SetValue(characterTable.characterMoney.characterMoney, Mathf.Max(0, serverBalance - pending));
            SetValue(characterTable.characterMoney.characterBet, ConfirmedTotal() + pending);

            bool open = phase == Phase.Betting && !cleaning;
            if (characterTable.currentTableActive.Value != open) characterTable.currentTableActive.Value = open;

            RefreshTimer();
        }

        static void SetValue(IntReactiveProperty property, int value)
        {
            if (property.Value != value) property.Value = value;
        }

        // ------------------------------------------------------------------ the round

        IEnumerator GameLoop()
        {
            // Constants.userData is filled by the login
            if (Constants.userData == null)
            {
                ShowMessage("Waiting for login...");
                yield return new WaitUntil(() => Constants.userData != null);
            }
            serverBalance = Constants.userData.balance;
            // marks the player online (the server's auto-result cron only runs while nobody is online)
            StartCoroutine(SendStatus("yes"));
            yield return RefreshBalance(0);

            while (true)
            {
                yield return WaitForRound();
                yield return Betting();
                yield return NoMoreBets();
                yield return Spin();
                yield return ShowResult();
                yield return ClearTable();
            }
        }

        IEnumerator WaitForRound()
        {
            phase = Phase.Waiting;
            if (waitingPanel != null) waitingPanel.SetActive(true);

            while (true)
            {
                SessionResponse s = null;
                yield return GetSession(r => s = r);
                if (s != null)
                {
                    ShowHistory(s);
                    if (s.session != finishedSession && s.status == "pending" && s.diff >= minSecondsToJoin)
                    {
                        StartRound(s);
                        yield break;
                    }
                    // the current round is too far gone (or over): the next one starts after it
                    int nextIn = s.diff + s.nextRoundAfter;
                    waitingText.Set(nextIn > 0 ? "Next round in " + nextIn + "s" : "Next round starting...");
                }
                else
                {
                    waitingText.Set("Connecting...");
                }
                yield return new WaitForSecondsRealtime(2f);
            }
        }

        void StartRound(SessionResponse s)
        {
            Constants.sessionData = s;
            session = s.session;
            roundId++;
            roundEndsAt = Time.realtimeSinceStartup + s.diff;
            ResultNumber = -1;
            confirmedTotals.Clear();

            sessionText.Set(s.sessionNum);
            if (waitingPanel != null) waitingPanel.SetActive(false);
            phase = Phase.Betting;
            Play(placeYourBetsClip);
            ShowMessage("Place your bets");
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
            ShowMessage("No more bets");

            // wait for the clock, and for a bet reply that may still be on its way
            while (SecondsLeft > 0 || betInFlight)
            {
                yield return null;
            }
            // chips that were never sent are not in the game: give them back
            if (RemoveUnsentChips() > 0) ShowMessage("Chips not sent were removed");
        }

        /// <summary>
        /// Every phone starts the wheel when the clock reaches 0 and follows the same timeline, so the
        /// ball lands at the same moment everywhere. The result is asked for in parallel.
        /// </summary>
        IEnumerator Spin()
        {
            phase = Phase.Spinning;
            finishedSession = session;
            ResultNumber = -1;
            resultFailed = false;
            StartCoroutine(FetchResult(session, roundId));

            characterTable.OnRound.OnNext(true);
            gameRoullete.OnRotate.OnNext(true);
            PlaySound(7);

            yield return new WaitForSecondsRealtime(2.0f);
            gameRoullete.currentSpeed = 75f;
            yield return new WaitForSecondsRealtime(1.0f);
            gameRoullete.currentSpeed = 145f;
            PlaySound(9);
            yield return new WaitForSecondsRealtime(0.5f);
            gameRoullete.currentSpeed = 240f;
            yield return new WaitForSecondsRealtime(1.2f);
            gameRoullete.currentSpeed = 245f;
            yield return new WaitForSecondsRealtime(2.0f);
            gameRoullete.currentSpeed = 265f;
            yield return new WaitForSecondsRealtime(3.8f);
            gameRoullete.currentSpeed = 245f;
            yield return new WaitForSecondsRealtime(1.5f);
            gameRoullete.currentSpeed = 240f;
            yield return new WaitForSecondsRealtime(1.5f);

            // the ball drops on the server's number (keeps spinning if it isn't here yet)
            while (ResultNumber < 0 && !resultFailed)
            {
                yield return null;
            }
            gameRoullete.currentSpeed = 145f;
            if (ResultNumber >= 0)
            {
                characterTable.lastNumber = ResultNumber;
                gameRoullete.OnNumber.OnNext(ResultNumber);     // ball, big number, last numbers strip
                stripSessions.Add(finishedSession);
            }

            yield return new WaitForSecondsRealtime(1.8f);
            gameRoullete.currentSpeed = 75f;
            yield return new WaitForSecondsRealtime(5.0f);
            gameRoullete.currentSpeed = gameRoullete.defaultSpeed;
            characterTable.OnRound.OnNext(false);
        }

        IEnumerator FetchResult(string forSession, int forRound)
        {
            float giveUpAt = Time.realtimeSinceStartup + resultTimeout;
            while (forRound == roundId && Time.realtimeSinceStartup < giveUpAt)
            {
                WWWForm form = new WWWForm();
                form.AddField("session", forSession);
                using (UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriWinnumber, form))
                {
                    request.timeout = 15;
                    yield return request.SendWebRequest();
                    if (request.responseCode == 200)
                    {
                        WinResponse win = JsonUtility.FromJson<WinResponse>(request.downloadHandler.text);
                        if (win != null && win.number >= 0 && win.number <= 36 && forRound == roundId)
                        {
                            ResultNumber = win.number;
                            yield break;
                        }
                    }
                }
                // 202 "Result not ready" (clock a little ahead of the server) or a network error
                yield return new WaitForSecondsRealtime(1f);
            }
            if (forRound == roundId) resultFailed = true;
        }

        IEnumerator ShowResult()
        {
            phase = Phase.Result;

            if (ResultNumber < 0)
            {
                ShowMessage("Result not received - check your bet history");
                yield return RefreshBalance(0);
                yield break;
            }

            if (BallLanded != null)
            {
                try { BallLanded(ResultNumber); }
                catch (Exception e) { Debug.LogException(e); }
            }

            // like the original game: the payment shows 3s after the wheel stops
            yield return new WaitForSecondsRealtime(3f);

            int staked = ConfirmedTotal();
            int win = Payout(confirmedTotals, ResultNumber);
            characterTable.OnWinButton.OnNext(ResultNumber);
            ShowMessage(ResultNumber + " " + ColorOf(ResultNumber).ToUpperInvariant());
            if (staked > 0)
            {
                // WinLostDisplay: "You win!" with what was paid, or "You lost!" with the stake
                characterTable.characterMoney.currentPayment.Value = win > 0 ? win : -staked;
            }

            // the server paid when the result was made; show the new balance only now
            yield return RefreshBalance(win);
        }

        /// <summary>Sweeps the chips off (no refunds: the round is settled) and remembers them for Repeat.</summary>
        IEnumerator ClearTable()
        {
            cleaning = true;

            List<RepeatChip> mine = placed.Values
                .Where(p => p.confirmed && p.chip != null && p.button != null)
                .Select(p => new RepeatChip { chip = p.chip, button = p.button })
                .ToList();
            if (mine.Count > 0)
            {
                lastRound.Clear();
                lastRound.AddRange(mine);
            }

            bool sweep = magnet != null && gameCmdFactory != null && characterTable.currentTable.Count > 0;
            if (sweep)
            {
                // the magnet only runs when the table counts chips (the game resets the count 2s after the spin)
                characterTable.currentTableCount = characterTable.currentTable.Count;
                gameCmdFactory.ResetTableTurn(magnet, characterTable, 0).Execute();
            }
            characterTable.currentTableCount = 0;            // chips destroyed from now on are not refunded
            characterTable.currentTableInGame.Clear();
            if (sweep)
            {
                yield return new WaitForSecondsRealtime(magnet.magnetTime + 0.5f);
            }

            foreach (ChipGame chip in characterTable.currentTable.ToList())
            {
                if (chip != null) Remove(chip);
            }
            yield return null;                               // Destroy happens at the end of the frame
            characterTable.currentTable.RemoveAll(c => c == null);
            placed.Clear();
            removing.Clear();
            confirmedTotals.Clear();
            cleaning = false;
        }

        // ------------------------------------------------------------------ buttons

        /// <summary>Bet button (the old Play button): sends every chip not sent yet, in one request.</summary>
        public void PlaceBet()
        {
            if (phase != Phase.Betting)
            {
                ShowMessage(phase == Phase.Waiting ? "Wait for the next round" : "Betting is closed");
                return;
            }
            if (betInFlight)
            {
                return;
            }
            List<ChipGame> slip = placed.Where(p => !p.Value.confirmed && p.Key != null).Select(p => p.Key).ToList();
            if (slip.Count == 0)
            {
                ShowMessage(confirmedTotals.Count > 0 ? "Bet already placed" : "Put chips on the table first");
                return;
            }

            // the server takes this round's totals per spot (already accepted + new chips)
            Dictionary<string, int> totals = new Dictionary<string, int>(confirmedTotals);
            foreach (ChipGame chip in slip)
            {
                PlacedChip p = placed[chip];
                int current;
                totals.TryGetValue(p.key, out current);
                totals[p.key] = current + p.value;
            }

            betInFlight = true;
            StartCoroutine(SendBet(totals, slip, roundId, session));
        }

        /// <summary>Undo button: removes the last chip, if it hasn't been sent.</summary>
        public void UndoLastChip()
        {
            if (phase != Phase.Betting || betInFlight) return;
            List<ChipGame> table = characterTable.currentTable;
            if (table.Count == 0) return;
            ChipGame last = table[table.Count - 1];
            PlacedChip p;
            if (last != null && placed.TryGetValue(last, out p) && p.confirmed)
            {
                ShowMessage("Placed bets can't be taken back");
                return;
            }
            if (last != null && !removing.Contains(last)) Remove(last);
        }

        /// <summary>Clear button: removes every chip not sent yet.</summary>
        public void ClearUnsentChips()
        {
            if (phase != Phase.Betting || betInFlight) return;
            if (RemoveUnsentChips() == 0 && confirmedTotals.Count > 0) ShowMessage("Placed bets can't be taken back");
        }

        /// <summary>Repeat button: puts last round's bets on the table again (not sent until Bet).</summary>
        public void RepeatLastBet()
        {
            if (phase != Phase.Betting || betInFlight) return;
            if (lastRound.Count == 0 || gameCmdFactory == null)
            {
                ShowMessage("No previous bet to repeat");
                return;
            }

            Chip selected = characterTable.currentChipSelected;
            int skipped = 0;
            foreach (RepeatChip r in lastRound)
            {
                if (r.chip.chipkey == KeyFicha.ChipAll || r.chip.chipValue > serverBalance - PendingValue())
                {
                    skipped++;
                    continue;
                }
                characterTable.currentChipSelected = r.chip;
                GameObject chipInstance = Instantiate(characterTable.chipPrefab);
                chipInstance.SetActive(false);
                gameCmdFactory.ButtonTableTurn(characterTable, chipInstance, r.button).Execute();
                TrackChips();
            }
            characterTable.currentChipSelected = selected;
            if (skipped > 0) ShowMessage("Not enough balance to repeat every chip");
        }

        public void CloseLowBalancePopup()
        {
            if (lowBalancePopup != null) lowBalancePopup.SetActive(false);
        }

        IEnumerator SendBet(Dictionary<string, int> totals, List<ChipGame> slip, int sentRound, string sentSession)
        {
            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);
            form.AddField("session", sentSession);
            form.AddField("bets", string.Join(",", totals.Select(t => t.Key + ":" + t.Value)));
            form.AddField("amount", totals.Values.Sum());

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

            int balance;
            if (code == 200 && TryParseBetReply(body, out balance))
            {
                confirmedTotals.Clear();
                foreach (KeyValuePair<string, int> t in totals) confirmedTotals[t.Key] = t.Value;
                foreach (ChipGame chip in slip)
                {
                    PlacedChip p;
                    if (chip != null && placed.TryGetValue(chip, out p)) p.confirmed = true;
                }
                serverBalance = balance;
                ShowMessage("Bet placed");
            }
            else if (body == "low_balance")
            {
                RemoveUnsentChips();
                ShowLowBalance();
                yield return RefreshBalance(0);
            }
            else if (body == "closed")
            {
                RemoveUnsentChips();
                ShowMessage("Betting closed");
            }
            else if (code == 422)
            {
                RemoveUnsentChips();
                ShowMessage("Bet not accepted");
            }
            else
            {
                // network/server error: keep the chips so the player can press Bet again (safe to resend)
                if (phase != Phase.Betting) RemoveUnsentChips();
                ShowMessage("Bet not placed, please try again");
            }
        }

        // ------------------------------------------------------------------ chips

        /// <summary>Notes every new chip on the table (its spot and value at that moment).</summary>
        void TrackChips()
        {
            List<ChipGame> table = characterTable.currentTable;

            List<ChipGame> gone = null;
            foreach (ChipGame chip in placed.Keys)
            {
                if (chip == null || !table.Contains(chip)) (gone ?? (gone = new List<ChipGame>())).Add(chip);
            }
            if (gone != null) foreach (ChipGame chip in gone) placed.Remove(chip);
            removing.RemoveWhere(chip => chip == null);

            List<ChipGame> rejected = null;
            for (int i = 0; i < table.Count; i++)
            {
                ChipGame chip = table[i];
                if (chip == null || placed.ContainsKey(chip) || removing.Contains(chip)) continue;
                IChipRuntime runtime = chip._chipRuntime;
                if (runtime == null || runtime.currentButton == null || runtime.currentChipData == null) continue;

                string key = KeyOf(runtime.currentButton.buttonValue);
                if (phase != Phase.Betting || cleaning || !IsLegalBet(key))
                {
                    (rejected ?? (rejected = new List<ChipGame>())).Add(chip);
                    continue;
                }
                placed[chip] = new PlacedChip
                {
                    key = key,
                    value = runtime.currentChipData.chipValue,
                    chip = runtime.currentChipData,
                    button = runtime.currentButton,
                };
            }
            if (rejected != null)
            {
                foreach (ChipGame chip in rejected) Remove(chip);
                ShowMessage(phase == Phase.Betting ? "No bets on that spot" : "Betting is closed");
            }
        }

        int RemoveUnsentChips()
        {
            List<ChipGame> unsent = placed.Where(p => !p.Value.confirmed && p.Key != null).Select(p => p.Key).ToList();
            foreach (ChipGame chip in unsent) Remove(chip);
            return unsent.Count;
        }

        /// <summary>Takes a chip off the table (TableController gives its value back to the money shown).</summary>
        void Remove(ChipGame chip)
        {
            placed.Remove(chip);
            removing.Add(chip);
            chip.DestroyChip();
        }

        int PendingValue()
        {
            int sum = 0;
            foreach (KeyValuePair<ChipGame, PlacedChip> p in placed)
            {
                if (!p.Value.confirmed && p.Key != null) sum += p.Value.value;
            }
            return sum;
        }

        int ConfirmedTotal()
        {
            int sum = 0;
            foreach (int v in confirmedTotals.Values) sum += v;
            return sum;
        }

        // ------------------------------------------------------------------ rules (same as the server)

        static readonly int[] Red = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        static HashSet<string> legalBets;

        public static string ColorOf(int number)
        {
            if (number == 0) return "green";
            return Array.IndexOf(Red, number) >= 0 ? "red" : "black";
        }

        /// <summary>The spot's numbers in order, "10-11": what the server receives.</summary>
        public static string KeyOf(IEnumerable<int> numbers)
        {
            return string.Join("-", numbers.Distinct().OrderBy(n => n).Select(n => n.ToString()).ToArray());
        }

        /// <summary>Straight, split, street, corner, six line, zero bets, dozens, columns, 1-18/19-36, even/odd, red/black.</summary>
        public static bool IsLegalBet(string key)
        {
            if (legalBets == null)
            {
                HashSet<string> bets = new HashSet<string>();
                for (int n = 0; n <= 36; n++) bets.Add(KeyOf(new[] { n }));
                for (int n = 1; n <= 36; n++)
                {
                    if (n % 3 != 0) bets.Add(KeyOf(new[] { n, n + 1 }));
                    if (n <= 33) bets.Add(KeyOf(new[] { n, n + 3 }));
                    if (n % 3 == 1) bets.Add(KeyOf(new[] { n, n + 1, n + 2 }));
                    if (n % 3 != 0 && n <= 32) bets.Add(KeyOf(new[] { n, n + 1, n + 3, n + 4 }));
                    if (n % 3 == 1 && n <= 31) bets.Add(KeyOf(Enumerable.Range(n, 6)));
                }
                bets.Add("0-1"); bets.Add("0-2"); bets.Add("0-3"); bets.Add("0-1-2"); bets.Add("0-2-3"); bets.Add("0-1-2-3");
                for (int i = 1; i <= 3; i++)
                {
                    bets.Add(KeyOf(Enumerable.Range(12 * i - 11, 12)));                         // dozen
                    bets.Add(KeyOf(Enumerable.Range(0, 12).Select(k => i + 3 * k)));           // column
                }
                bets.Add(KeyOf(Enumerable.Range(1, 18)));
                bets.Add(KeyOf(Enumerable.Range(19, 18)));
                bets.Add(KeyOf(Enumerable.Range(1, 18).Select(k => 2 * k)));
                bets.Add(KeyOf(Enumerable.Range(0, 18).Select(k => 2 * k + 1)));
                bets.Add(KeyOf(Red));
                bets.Add(KeyOf(Enumerable.Range(1, 36).Where(n => Array.IndexOf(Red, n) < 0)));
                legalBets = bets;
            }
            return legalBets.Contains(key);
        }

        /// <summary>What these bets pay (stake included) on this number: 36 / numbers covered per spot.</summary>
        public static int Payout(Dictionary<string, int> bets, int number)
        {
            int won = 0;
            foreach (KeyValuePair<string, int> bet in bets)
            {
                string[] parts = bet.Key.Split('-');
                if (Array.IndexOf(parts, number.ToString()) >= 0) won += bet.Value * (36 / parts.Length);
            }
            return won;
        }

        // ------------------------------------------------------------------ helpers

        void ShowHistory(SessionResponse s)
        {
            if (lastNumbers == null || s.bets == null || s.bets.Length == 0) return;
            // only when the strip is missing the newest result (first start, rounds this phone didn't see)
            if (stripSessions.Contains(s.bets[0].sessionId)) return;
            lastNumbers.ShowHistory(s.bets.Select(b => b.number).ToArray());
            stripSessions.Clear();
            foreach (ResultRow b in s.bets) stripSessions.Add(b.sessionId);
        }

        void RefreshTimer()
        {
            if (phase == Phase.Waiting)
            {
                timerText.Set("--");
                return;
            }
            int left = (phase == Phase.Betting || phase == Phase.Closed) ? SecondsLeft : 0;
            timerText.Set(left.ToString("00"));
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
            MessageArea.SetActive(true);
            messageText.Set(message);
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(ClearMessageAfter(messageSeconds));
        }

        IEnumerator ClearMessageAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            MessageArea.SetActive(false);
            messageText.Set("");
        }

        void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        /// <summary>The project's sound list (PlayerSound): 7 = spin starts, 9 = ball running.</summary>
        static void PlaySound(int index)
        {
            PlayerSound sound = PlayerSound.Instance;
            if (sound == null || sound.gameSound == null || sound.gameSound.audioReferences == null) return;
            if (index < sound.gameSound.audioReferences.Length) sound.gameSound.OnSound.OnNext(sound.gameSound.audioReferences[index]);
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
                    serverBalance = balance;
                }
                else if (fallbackWin > 0)
                {
                    serverBalance += fallbackWin;
                }
                if (Constants.userData != null) Constants.userData.balance = serverBalance;
            }
        }

        static bool TryParseBetReply(string body, out int balance)
        {
            balance = 0;
            string[] parts = body.Split('|');
            int play;
            return parts.Length >= 2 && int.TryParse(parts[0].Trim(), out balance) && int.TryParse(parts[1].Trim(), out play);
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
            // same round: correct the clock; the round is over: go straight to its spin
            roundEndsAt = Time.realtimeSinceStartup + (s.session == session ? s.diff : 0);
        }
    }
}
