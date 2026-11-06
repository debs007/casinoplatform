using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace bigsmall
{
    /// <summary>
    /// Big Small result history (separate from BigSmallGame, only reads Constants).
    ///
    /// 1) Small / 7 / Big boxes: the latest results of each area as dice face images. Each box is
    ///    one object (e.g. your Horizontal Layout Group) whose children are the face Images:
    ///    1st + 2nd = latest result (left), 3rd + 4th = the one before, 5th + 6th = oldest.
    ///    Loaded from the server at start; when the dice stop, the new result goes in on the left
    ///    of its area's box and the others move right.
    /// 2) Today's results panel: every result of today, newest first, one row per round.
    ///
    /// Put this on an object that stays active (e.g. the same object as BigSmallGame).
    /// </summary>
    [DisallowMultipleComponent]
    public class ResultHistory : MonoBehaviour
    {
        [Header("Dice face sprites: element 0 = face 1 ... element 5 = face 6")]
        public Sprite[] diceFaces = new Sprite[6];

        [Header("Latest results per area: the object holding the 6 face Images, in order")]
        [Tooltip("e.g. smallbox > area > HorizontalLayoutGroup. Its children: face1 + face2 = latest, face3 + face4, face5 + face6 = oldest")]
        public Transform smallFaces;
        public Transform sevenFaces;
        public Transform bigFaces;
        [Tooltip("Optional: found automatically. Its dice-stopped event adds the new result.")]
        public BigSmallGame game;

        [Header("Today's results panel")]
        public GameObject todayPanel;
        public Button openButton;
        public Button closeButton;
        [Tooltip("Content object of the panel's ScrollRect (with a Vertical Layout Group)")]
        public RectTransform listContent;
        public ScrollRect scrollRect;
        [Tooltip("One row. Children found by name: Dice1, Dice2 (Image), Number, Area, Time, Round (Text or TextMeshPro). Any can be left out.")]
        public GameObject rowPrefab;
        [Tooltip("Optional: how often each area won today, written with Summary Format")]
        public BigSmallGame.TextRef summaryText = new BigSmallGame.TextRef();
        [Tooltip("{0} = Small wins, {1} = 7 wins, {2} = Big wins")]
        public string summaryFormat = "SMALL {0}   SEVEN {1}   BIG {2}";
        [Tooltip("Optional: today's date")]
        public BigSmallGame.TextRef dateText = new BigSmallGame.TextRef();
        [Tooltip("Optional: shown while the list loads the first time")]
        public GameObject loadingIndicator;
        [Tooltip("Optional: shown when there is no result yet today")]
        public GameObject emptyLabel;
        [Tooltip("Rows created per frame the first time (a full day is ~860 rows)")]
        public int rowsPerFrame = 40;

        [Header("Area names and colours in the rows")]
        public string smallLabel = "SMALL";
        public string sevenLabel = "7";
        public string bigLabel = "BIG";
        public bool colorAreaText = true;
        public Color smallColor = new Color(0.25f, 0.6f, 1f);
        public Color sevenColor = new Color(1f, 0.8f, 0.2f);
        public Color bigColor = new Color(1f, 0.35f, 0.3f);

        [Header("Server")]
        public string areaHistoryEndpoint = "areaHistory";
        public string todayResultsEndpoint = "todayResults";
        public int retries = 3;
        public float retrySeconds = 3f;

        /// <summary>Latest results per area as last received, newest first (empty until loaded).</summary>
        public AreaHistoryResponse Areas { get; private set; }
        /// <summary>Today's results as last received, newest first (null until the panel is opened).</summary>
        public TodayResultsResponse Today { get; private set; }

        class RowView
        {
            public GameObject go;
            public Image dice1, dice2;
            public Graphic number, area, time, round;
        }

        readonly List<RowView> rows = new List<RowView>();
        Image[] smallImages, sevenImages, bigImages;
        string lastSession;
        int areaRequest;    // newer request wins: a slow older reply is dropped
        int todayRequest;
        bool started;

        void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(OpenTodayPanel);
            if (closeButton != null) closeButton.onClick.AddListener(CloseTodayPanel);
            if (todayPanel != null) todayPanel.SetActive(false);
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            if (emptyLabel != null) emptyLabel.SetActive(false);
            smallImages = FaceImages(smallFaces);
            sevenImages = FaceImages(sevenFaces);
            bigImages = FaceImages(bigFaces);
            Areas = new AreaHistoryResponse();
            ShowAreas(Areas);
        }

        void Start()
        {
            started = true;
            lastSession = CurrentSession();
            if (game == null) game = BigSmallGame.Instance;
            if (game != null) game.DiceStopped += AddResult;
            else Debug.LogWarning("ResultHistory: no BigSmallGame found, boxes update only when a new round starts");
            RefreshAreas();
        }

        void OnDestroy()
        {
            if (game != null) game.DiceStopped -= AddResult;
        }

        void OnEnable()
        {
            // coroutines stop while this object is inactive: catch up when it comes back
            if (started) RefreshAreas();
        }

        void Update()
        {
            // a new round started: reload from the server (also covers rounds this client skipped)
            string session = CurrentSession();
            if (session == lastSession) return;
            lastSession = session;
            RefreshAreas();
            if (todayPanel != null && todayPanel.activeSelf) RefreshToday();
        }

        static string CurrentSession()
        {
            return Constants.sessionData != null ? Constants.sessionData.session : null;
        }

        // ---------------------------------------------------------------- public

        /// <summary>Reloads the latest results of each area from the server.</summary>
        public void RefreshAreas()
        {
            int id = ++areaRequest;
            int count = Mathf.Max(1, Mathf.Max(Pairs(smallImages), Mathf.Max(Pairs(sevenImages), Pairs(bigImages))));
            StartCoroutine(Fetch<AreaHistoryResponse>(areaHistoryEndpoint, () => id == areaRequest, r =>
            {
                Areas = r;
                ShowAreas(r);
            }, null, count));
        }

        /// <summary>
        /// Puts a new result on the left of its area's box (the others move right) and on top of
        /// today's list. Called by BigSmallGame when the dice stop; can also be called by hand.
        /// </summary>
        public void AddResult(int number, int dice1, int dice2)
        {
            if (number < 2 || number > 12) return;
            if (dice1 < 1 || dice1 > 6 || dice2 < 1 || dice2 > 6 || dice1 + dice2 != number)
            {
                dice1 = number / 2;              // server faces missing: a pair that adds up
                dice2 = number - dice1;
            }
            HistoryResult r = new HistoryResult
            {
                sessionId = CurrentSession(),
                sessionNum = Constants.sessionData != null ? Constants.sessionData.sessionNum : "",
                number = number,
                dice = dice1 + "," + dice2,
                dice1 = dice1,
                dice2 = dice2,
                area = number <= 6 ? "small" : (number == 7 ? "seven" : "big"),
                time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            };

            // a reply already on its way was asked before this result: it must not undo it
            areaRequest++;
            if (r.area == "small") Areas.small = Prepend(Areas.small, r);
            else if (r.area == "seven") Areas.seven = Prepend(Areas.seven, r);
            else Areas.big = Prepend(Areas.big, r);
            ShowAreas(Areas);

            if (Today != null)
            {
                int before = Today.results != null ? Today.results.Length : 0;
                Today.results = Prepend(Today.results, r, 2000);
                if (Today.results.Length > before)
                {
                    if (r.area == "small") Today.small++;
                    else if (r.area == "seven") Today.seven++;
                    else Today.big++;
                }
                int id = ++todayRequest;
                if (todayPanel != null && todayPanel.activeSelf) StartCoroutine(ShowToday(Today, id));
            }
        }

        static HistoryResult[] Prepend(HistoryResult[] list, HistoryResult r, int max = 20)
        {
            if (list == null) list = new HistoryResult[0];
            if (list.Length > 0 && list[0] != null && !string.IsNullOrEmpty(r.sessionId) && list[0].sessionId == r.sessionId)
                return list;                     // already there
            List<HistoryResult> all = new List<HistoryResult>(list.Length + 1) { r };
            all.AddRange(list);
            if (all.Count > max) all.RemoveRange(max, all.Count - max);
            return all.ToArray();
        }

        /// <summary>Reloads today's results into the panel's list.</summary>
        public void RefreshToday()
        {
            int id = ++todayRequest;
            if (loadingIndicator != null) loadingIndicator.SetActive(Today == null);
            StartCoroutine(Fetch<TodayResultsResponse>(todayResultsEndpoint, () => id == todayRequest, r =>
            {
                Today = r;
                StartCoroutine(ShowToday(r, id));
            }, () =>
            {
                if (loadingIndicator != null) loadingIndicator.SetActive(false);
            }));
        }

        public void OpenTodayPanel()
        {
            game.DiceArea.SetActive(false);
            if (todayPanel != null) todayPanel.SetActive(true);
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
            RefreshToday();

        }

        public void CloseTodayPanel()
        {
            game.DiceArea.SetActive(true);
            if (todayPanel != null) todayPanel.SetActive(false);
        }

        /// <summary>The sprite for a dice face 1-6 (null if not assigned).</summary>
        public Sprite DiceSprite(int face)
        {
            if (diceFaces == null || face < 1 || face > diceFaces.Length) return null;
            return diceFaces[face - 1];
        }

        // ---------------------------------------------------------------- area boxes

        void ShowAreas(AreaHistoryResponse r)
        {
            FillFaces(smallImages, r.small);
            FillFaces(sevenImages, r.seven);
            FillFaces(bigImages, r.big);
        }

        /// <summary>Images 0+1 = results[0] (latest), 2+3 = results[1], ... An image with no result is hidden (its place stays).</summary>
        void FillFaces(Image[] images, HistoryResult[] results)
        {
            if (images == null) return;
            for (int i = 0; i < images.Length; i++)
            {
                int index = i / 2;
                HistoryResult result = results != null && index < results.Length ? results[index] : null;
                int face = result == null ? 0 : (i % 2 == 0 ? result.dice1 : result.dice2);
                SetDice(images[i], face);
            }
        }

        static int Pairs(Image[] images)
        {
            return images != null ? images.Length / 2 : 0;
        }

        /// <summary>The face Image of each child of the box, in the order shown (left to right).</summary>
        static Image[] FaceImages(Transform box)
        {
            List<Image> images = new List<Image>();
            if (box == null) return images.ToArray();
            foreach (Transform child in box)
            {
                Image image = FirstImage(child);
                if (image != null) images.Add(image);
            }
            return images.ToArray();
        }

        static Image FirstImage(Transform t)
        {
            Image image = t.GetComponent<Image>();
            if (image != null) return image;
            foreach (Transform child in t)
            {
                image = FirstImage(child);
                if (image != null) return image;
            }
            return null;
        }

        void SetDice(Image image, int face)
        {
            if (image == null) return;
            Sprite sprite = DiceSprite(face);
            if (sprite != null) image.sprite = sprite;
            image.enabled = sprite != null;
        }

        // ---------------------------------------------------------------- today's list

        IEnumerator ShowToday(TodayResultsResponse r, int id)
        {
            HistoryResult[] results = r.results ?? new HistoryResult[0];

            summaryText.Set(string.Format(summaryFormat, r.small, r.seven, r.big));
            DateTime day;
            dateText.Set(DateTime.TryParseExact(r.date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day)
                ? day.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : r.date);
            if (emptyLabel != null) emptyLabel.SetActive(results.Length == 0);

            if (rowPrefab != null && listContent != null)
            {
                int made = 0;
                for (int i = 0; i < results.Length; i++)
                {
                    if (i >= rows.Count)
                    {
                        rows.Add(MakeRow());
                        if (++made % Mathf.Max(1, rowsPerFrame) == 0)
                        {
                            yield return null;
                            if (id != todayRequest) yield break;     // a newer list is on its way
                        }
                    }
                    FillRow(rows[i], results[i]);
                }
                for (int i = results.Length; i < rows.Count; i++) rows[i].go.SetActive(false);
            }

            if (loadingIndicator != null) loadingIndicator.SetActive(false);
        }

        RowView MakeRow()
        {
            GameObject go = Instantiate(rowPrefab, listContent, false);
            RowView view = new RowView
            {
                go = go,
                dice1 = Find<Image>(go.transform, "Dice1"),
                dice2 = Find<Image>(go.transform, "Dice2"),
                number = FindText(go.transform, "Number"),
                area = FindText(go.transform, "Area"),
                time = FindText(go.transform, "Time"),
                round = FindText(go.transform, "Round"),
            };
            if (view.round == null) view.round = FindText(go.transform, "Session");
            return view;
        }

        void FillRow(RowView row, HistoryResult r)
        {
            row.go.SetActive(true);
            SetDice(row.dice1, r.dice1);
            SetDice(row.dice2, r.dice2);
            SetText(row.number, r.number.ToString());
            SetText(row.time, r.time);
            SetText(row.round, r.sessionNum);
            SetText(row.area, AreaLabel(r.area));
            if (colorAreaText && row.area != null) row.area.color = AreaColor(r.area);
        }

        string AreaLabel(string area)
        {
            if (area == "small") return smallLabel;
            if (area == "seven") return sevenLabel;
            if (area == "big") return bigLabel;
            return area;
        }

        Color AreaColor(string area)
        {
            if (area == "small") return smallColor;
            if (area == "seven") return sevenColor;
            return bigColor;
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

        static T Find<T>(Transform parent, string name) where T : Component
        {
            Transform child = FindChild(parent, name);
            return child != null ? child.GetComponent<T>() : null;
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

        IEnumerator Fetch<T>(string endpoint, Func<bool> stillWanted, Action<T> done, Action failed = null, int count = 0) where T : class
        {
            for (int attempt = 0; attempt <= retries; attempt++)
            {
                if (attempt > 0) yield return new WaitForSecondsRealtime(retrySeconds);
                if (!stillWanted()) yield break;

                WWWForm form = new WWWForm();
                if (Constants.userData != null && !string.IsNullOrEmpty(Constants.userData.id))
                    form.AddField("id", Constants.userData.id);     // rate limit counts per player
                if (count > 0) form.AddField("count", count);

                T reply = null;
                using (UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriApi + endpoint, form))
                {
                    request.timeout = 15;
                    yield return request.SendWebRequest();
                    if (request.responseCode == 200) reply = JsonUtility.FromJson<T>(request.downloadHandler.text);
                }

                if (!stillWanted()) yield break;
                if (reply != null) { done(reply); yield break; }
            }
            Debug.LogWarning("ResultHistory: could not load " + endpoint);
            if (failed != null) failed();
        }
    }

    [Serializable]
    public class HistoryResult
    {
        public string sessionId;
        public string sessionNum;   // round number shown in game
        public int number;          // 2-12
        public string dice;         // "3,6"
        public int dice1;           // 3
        public int dice2;           // 6
        public string area;         // "small", "seven" or "big"
        public string time;         // "14:05:30"
    }

    [Serializable]
    public class AreaHistoryResponse
    {
        public HistoryResult[] small = new HistoryResult[0];
        public HistoryResult[] seven = new HistoryResult[0];
        public HistoryResult[] big = new HistoryResult[0];
    }

    [Serializable]
    public class TodayResultsResponse
    {
        public string date;
        public int small;           // how many times each area won today
        public int seven;
        public int big;
        public HistoryResult[] results = new HistoryResult[0];
    }
}