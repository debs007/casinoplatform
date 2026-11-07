using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace roulette
{
    /// <summary>
    /// Two optional panels:
    ///   Today's results - every result of today, newest first (round, time, number, colour).
    ///   My bets         - this player's own bets, newest first (round, time, bets, result, win).
    /// Rows are copies of a row prefab whose children are found by name (any can be left out):
    ///   Round, Time, Number, Color, Bets, Amount, Win, Status (Text or TextMeshPro) and NumberBg (Image,
    ///   tinted red / black / green). A round's result shows up once the next round has started.
    /// Put this on an object that stays active (e.g. the same object as RouletteGame).
    /// </summary>
    [DisallowMultipleComponent]
    public class RouletteHistory : MonoBehaviour
    {
        [Serializable]
        public class Panel
        {
            public GameObject panel;
            public Button openButton;
            public Button closeButton;
            [Tooltip("Content object of the panel's ScrollRect (with a Vertical Layout Group)")]
            public RectTransform listContent;
            public ScrollRect scrollRect;
            public GameObject rowPrefab;
            [Tooltip("Optional: shown while the list loads the first time")]
            public GameObject loadingIndicator;
            [Tooltip("Optional: shown when the list is empty")]
            public GameObject emptyLabel;

            [NonSerialized] public List<Row> rows = new List<Row>();
            [NonSerialized] public int request;
            [NonSerialized] public bool loaded;
        }

        public class Row
        {
            public GameObject go;
            public Graphic round, time, number, color, bets, amount, win, status;
            public Image numberBg;
        }

        [Header("Today's results")]
        public Panel today = new Panel();
        [Tooltip("Optional: {0} = red, {1} = black, {2} = zero (how often each came up today)")]
        public RouletteGame.TextRef todaySummaryText = new RouletteGame.TextRef();
        public string todaySummaryFormat = "RED {0}   BLACK {1}   ZERO {2}";
        public RouletteGame.TextRef todayDateText = new RouletteGame.TextRef();

        [Header("My bets")]
        public Panel myBets = new Panel();
        [Tooltip("Optional: {0} = total played, {1} = total won")]
        public RouletteGame.TextRef myBetsSummaryText = new RouletteGame.TextRef();
        public string myBetsSummaryFormat = "PLAYED {0}   WON {1}";
        public int myBetsLimit = 50;

        [Header("Look")]
        public Color redColor = new Color(0.78f, 0.16f, 0.16f);
        public Color blackColor = new Color(0.15f, 0.2f, 0.22f);
        public Color greenColor = new Color(0.18f, 0.49f, 0.2f);
        public string wonText = "WON";
        public string lostText = "LOST";
        public string pendingText = "WAITING";
        [Tooltip("Rows created per frame the first time (a full day is ~700 rounds)")]
        public int rowsPerFrame = 40;
        public int retries = 2;

        string lastSession;

        void Awake()
        {
            Wire(today, () => Open(today), () => Close(today));
            Wire(myBets, () => Open(myBets), () => Close(myBets));
        }

        void Wire(Panel p, Action open, Action close)
        {
            if (p.openButton != null) p.openButton.onClick.AddListener(() => open());
            if (p.closeButton != null) p.closeButton.onClick.AddListener(() => close());
            if (p.panel != null) p.panel.SetActive(false);
            if (p.loadingIndicator != null) p.loadingIndicator.SetActive(false);
            if (p.emptyLabel != null) p.emptyLabel.SetActive(false);
        }

        void Update()
        {
            // a new round: the last one's result is public now
            string session = Constants.sessionData != null ? Constants.sessionData.session : null;
            if (session == lastSession) return;
            lastSession = session;
            if (IsOpen(today)) RefreshToday();
            if (IsOpen(myBets)) RefreshMyBets();
        }

        static bool IsOpen(Panel p)
        {
            return p.panel != null && p.panel.activeSelf;
        }

        // ---------------------------------------------------------------- public

        public void OpenTodayResults() { Open(today); }
        public void CloseTodayResults() { Close(today); }
        public void OpenMyBets() { Open(myBets); }
        public void CloseMyBets() { Close(myBets); }

        void Open(Panel p)
        {
            if (p.panel != null) p.panel.SetActive(true);
            if (p.scrollRect != null) p.scrollRect.verticalNormalizedPosition = 1f;
            if (p == today) RefreshToday(); else RefreshMyBets();
        }

        void Close(Panel p)
        {
            if (p.panel != null) p.panel.SetActive(false);
        }

        public void RefreshToday()
        {
            int id = ++today.request;
            if (today.loadingIndicator != null) today.loadingIndicator.SetActive(!today.loaded);
            StartCoroutine(Fetch<TodayResults>(Constants.todayResults, null, () => id == today.request, r =>
            {
                today.loaded = true;
                todaySummaryText.Set(string.Format(todaySummaryFormat, r.red, r.black, r.green));
                DateTime day;
                todayDateText.Set(DateTime.TryParseExact(r.date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day)
                    ? day.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : r.date);
                ResultRow[] results = r.results ?? new ResultRow[0];
                StartCoroutine(ShowRows(today, id, results.Length, (row, i) => FillResult(row, results[i])));
            }, today));
        }

        public void RefreshMyBets()
        {
            if (Constants.userData == null) return;
            int id = ++myBets.request;
            if (myBets.loadingIndicator != null) myBets.loadingIndicator.SetActive(!myBets.loaded);
            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);
            form.AddField("limit", myBetsLimit);
            StartCoroutine(Fetch<MyBets>(Constants.getmyBets, form, () => id == myBets.request, r =>
            {
                myBets.loaded = true;
                myBetsSummaryText.Set(string.Format(myBetsSummaryFormat, r.totalPlayPoint, r.winPoint));
                MyBetRow[] bets = r.allBets ?? new MyBetRow[0];
                StartCoroutine(ShowRows(myBets, id, bets.Length, (row, i) => FillBet(row, bets[i])));
            }, myBets));
        }

        // ---------------------------------------------------------------- rows

        IEnumerator ShowRows(Panel p, int id, int count, Action<Row, int> fill)
        {
            if (p.emptyLabel != null) p.emptyLabel.SetActive(count == 0);
            if (p.rowPrefab != null && p.listContent != null)
            {
                int made = 0;
                for (int i = 0; i < count; i++)
                {
                    if (i >= p.rows.Count)
                    {
                        p.rows.Add(MakeRow(p));
                        if (++made % Mathf.Max(1, rowsPerFrame) == 0)
                        {
                            yield return null;
                            if (id != p.request) yield break;     // a newer list is on its way
                        }
                    }
                    p.rows[i].go.SetActive(true);
                    fill(p.rows[i], i);
                }
                for (int i = count; i < p.rows.Count; i++) p.rows[i].go.SetActive(false);
            }
            if (p.loadingIndicator != null) p.loadingIndicator.SetActive(false);
        }

        Row MakeRow(Panel p)
        {
            GameObject go = Instantiate(p.rowPrefab, p.listContent, false);
            Row row = new Row
            {
                go = go,
                round = FindText(go.transform, "Round"),
                time = FindText(go.transform, "Time"),
                number = FindText(go.transform, "Number"),
                color = FindText(go.transform, "Color"),
                bets = FindText(go.transform, "Bets"),
                amount = FindText(go.transform, "Amount"),
                win = FindText(go.transform, "Win"),
                status = FindText(go.transform, "Status"),
            };
            Transform bg = FindChild(go.transform, "NumberBg");
            if (bg != null) row.numberBg = bg.GetComponent<Image>();
            return row;
        }

        void FillResult(Row row, ResultRow r)
        {
            SetText(row.round, r.sessionNum);
            SetText(row.time, r.time);
            SetText(row.number, r.number.ToString());
            SetText(row.color, ColorName(r.color));
            if (row.numberBg != null) row.numberBg.color = ColorFor(r.color);
        }

        void FillBet(Row row, MyBetRow b)
        {
            SetText(row.round, b.sessionNum);
            SetText(row.time, b.time);
            SetText(row.amount, b.amount.ToString());
            SetText(row.bets, BetsText(b.positions));
            bool pending = b.number < 0;
            SetText(row.number, pending ? "-" : b.number.ToString());
            SetText(row.color, pending ? "" : ColorName(b.color));
            if (row.numberBg != null) row.numberBg.color = pending ? Color.gray : ColorFor(b.color);
            SetText(row.win, b.win.ToString());
            SetText(row.status, pending ? pendingText : (b.win > 0 ? wonText : lostText));
        }

        /// <summary>"17 x50, Red x100"</summary>
        static string BetsText(BetPosition[] positions)
        {
            if (positions == null || positions.Length == 0) return "";
            string[] parts = new string[positions.Length];
            for (int i = 0; i < positions.Length; i++) parts[i] = positions[i].bet + " x" + positions[i].amount;
            return string.Join(", ", parts);
        }

        static string ColorName(string color)
        {
            return color == "green" ? "ZERO" : (color ?? "").ToUpperInvariant();
        }

        Color ColorFor(string color)
        {
            if (color == "red") return redColor;
            if (color == "green") return greenColor;
            return blackColor;
        }

        static void SetText(Graphic g, string value)
        {
            Text text = g as Text;
            if (text != null) { text.text = value; return; }
            TMP_Text tmp = g as TMP_Text;
            if (tmp != null) tmp.text = value;
        }

        /// <summary>A child (any depth) with this name, or null.</summary>
        static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase)) return child;
                Transform found = FindChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        static Graphic FindText(Transform parent, string name)
        {
            Transform child = FindChild(parent, name);
            if (child == null) return null;
            Graphic g = child.GetComponent<Text>();
            if (g == null) g = child.GetComponent<TMP_Text>();
            return g;
        }

        // ---------------------------------------------------------------- server

        IEnumerator Fetch<T>(string url, WWWForm form, Func<bool> stillWanted, Action<T> done, Panel p) where T : class
        {
            for (int attempt = 0; attempt <= retries; attempt++)
            {
                if (attempt > 0) yield return new WaitForSecondsRealtime(2f);
                if (!stillWanted()) yield break;

                if (form == null)
                {
                    form = new WWWForm();
                    if (Constants.userData != null) form.AddField("id", Constants.userData.id);   // rate limit per player
                }
                T reply = null;
                using (UnityWebRequest request = UnityWebRequest.Post(url, form))
                {
                    request.timeout = 15;
                    yield return request.SendWebRequest();
                    if (request.responseCode == 200) reply = JsonUtility.FromJson<T>(request.downloadHandler.text);
                }

                if (!stillWanted()) yield break;
                if (reply != null) { done(reply); yield break; }
            }
            Debug.LogWarning("RouletteHistory: could not load " + url);
            if (p.loadingIndicator != null) p.loadingIndicator.SetActive(false);
        }
    }
}
