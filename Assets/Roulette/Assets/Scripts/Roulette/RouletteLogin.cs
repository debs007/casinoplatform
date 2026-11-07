using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace roulette
{
    /// <summary>
    /// Optional login panel (same login API as the other games). Skip it if your own login scene
    /// fills roulette.Constants.userData before the game scene opens.
    /// Fill the UI Text or the TextMeshPro version of each field, whichever you use.
    /// </summary>
    public class RouletteLogin : MonoBehaviour
    {
        public GameObject loginPanel;
        public InputField userName;
        public TMP_InputField userNameTmp;
        public InputField password;
        public TMP_InputField passwordTmp;
        public Button loginButton;
        public RouletteGame.TextRef errorText = new RouletteGame.TextRef();
        public bool rememberName = true;

        bool busy;

        void Start()
        {
            if (loginButton != null) loginButton.onClick.AddListener(Login);
            if (loginPanel != null) loginPanel.SetActive(Constants.userData == null);
            if (rememberName) SetName(PlayerPrefs.GetString("roulette_user", ""));
            errorText.Set("");
        }

        public void Login()
        {
            if (busy) return;
            string name = (userName != null ? userName.text : userNameTmp != null ? userNameTmp.text : "").Trim();
            string pass = password != null ? password.text : passwordTmp != null ? passwordTmp.text : "";
            if (name == "" || pass == "")
            {
                errorText.Set("Enter user name and password");
                return;
            }
            StartCoroutine(SendLogin(name, pass));
        }

        IEnumerator SendLogin(string name, string pass)
        {
            busy = true;
            errorText.Set("Logging in...");
            WWWForm form = new WWWForm();
            form.AddField("userName", name);
            form.AddField("password", pass);
            using (UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriLogin, form))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                busy = false;

                UserData user = request.responseCode == 200 ? Parse(request.downloadHandler.text) : null;
                if (user == null)
                {
                    errorText.Set(request.responseCode == 203 ? "Wrong user name or password" : "Can't reach the server");
                    yield break;
                }
                if (rememberName) PlayerPrefs.SetString("roulette_user", name);
                errorText.Set("");
                Constants.userData = user;
                if (loginPanel != null) loginPanel.SetActive(false);
            }
        }

        /// <summary>The server sends id and balance as numbers; read them even if JsonUtility leaves id empty.</summary>
        static UserData Parse(string json)
        {
            UserResponse response = JsonUtility.FromJson<UserResponse>(json);
            UserData user = response != null ? response.data : null;
            if (user == null) return null;
            if (string.IsNullOrEmpty(user.id))
            {
                Match id = Regex.Match(json, "\"id\"\\s*:\\s*\"?(\\d+)");
                if (!id.Success) return null;
                user.id = id.Groups[1].Value;
            }
            return user;
        }

        void SetName(string value)
        {
            if (userName != null) userName.text = value;
            if (userNameTmp != null) userNameTmp.text = value;
        }
    }
}
