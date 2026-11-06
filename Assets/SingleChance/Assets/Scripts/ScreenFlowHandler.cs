using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

namespace SingleChance
{
    public class ScreenFlowHandler : MonoBehaviour
    {

        //[DllImport("__Internal")]
        //private static extern string pickDate();
        //[DllImport("__Internal")]
        //private static extern string GetReturnedDate();

        public GameObject[] AllPopups;

        public Text StartDate, EndDate;
        public static ScreenFlowHandler screenFlow;
        GameObject publicHolder;
        public GameObject BackgroundBlur;

#if !UNITY_IOS
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();
        public void OnMinimizeButtonClick()
        {
            ShowWindow(GetActiveWindow(), 2);
        }
#endif
        public static ScreenFlowHandler instance;
        public Image MuteOpen;
        public Sprite muteImg, unmuteImg;
        public AudioSource[] allAudios;
        bool mute;
        private void Awake()
        {
            instance = this;
        }




        private void Start()
        {
            screenFlow = this;

        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                closePopup();
            }
        }

        public void MuteUnmute()
        {
            if (mute)
            {
                mute = false;
                MuteOpen.sprite = unmuteImg;
                foreach (AudioSource src in allAudios)
                {
                    src.mute = false;
                }
            }
            else
            {
                mute = true;
                MuteOpen.sprite = muteImg;
                foreach (AudioSource src in allAudios)
                {
                    src.mute = true;
                }
            }

        }

        public void Shrink(GameObject obj)
        {
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 70f);
        }

        public void Resize(GameObject obj)
        {
            obj.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 80f);
        }

        private void OnApplicationPause(bool pause)
        {

            if (ServerConnector.serverConnector.GameIsStarted)
            {

                if (pause)
                {
                    StartCoroutine(SetOnlineStatus("paused"));
                }
                else
                {
                    StartCoroutine(SetOnlineStatus("yes"));
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (ServerConnector.serverConnector.GameIsStarted)
            {
                StartCoroutine(SetOnlineStatus("no"));
            }

        }

        public void close()
        {
            Application.Quit();
        }

        public void ReloadScene()
        {
            StartCoroutine(SetOnlineStatus("no"));
            SceneManager.LoadSceneAsync(1);
        }



        public IEnumerator SetOnlineStatus(string status)
        {
            WWWForm form = new WWWForm();
            form.AddField("id", Constants.userData.id);
            form.AddField("data", status);

            UnityWebRequest request = UnityWebRequest.Post(Constants.baseUriStatus, form);
            yield return request.SendWebRequest();
            Debug.LogError(request.downloadHandler.text);


        }


        public void OpenPopup(string popupName)
        {

            if (popupName.Equals("info"))
            {
                BackgroundBlur.SetActive(true);
                AllPopups[0].SetActive(true);
                publicHolder = AllPopups[0];
            }
            if (popupName.Equals("search"))
            {
                BackgroundBlur.SetActive(true);
                AllPopups[1].SetActive(true);
                publicHolder = AllPopups[1];
            }
            if (popupName.Equals("result"))
            {
                BackgroundBlur.SetActive(true);
                AllPopups[2].SetActive(true);
                publicHolder = AllPopups[2];
                Debug.Log("Should be activated");
                ResultView.resultView.VewingMainResult();
            }
            if (popupName.Equals("Err"))
            {
                BackgroundBlur.SetActive(true);
                AllPopups[3].SetActive(true);
                publicHolder = AllPopups[3];
            }

            if (popupName.Equals("NoInternet"))
            {
                BackgroundBlur.SetActive(true);
                AllPopups[4].SetActive(true);
                publicHolder = AllPopups[4];
                //Time.timeScale = 0;
            }

            if (popupName.Equals("close"))
            {
                //BackgroundBlur.SetActive(true);
                AllPopups[5].SetActive(true);
                publicHolder = AllPopups[5];
                //Time.timeScale = 0;
            }
        }

        //public void OpenDatePicker(string point)
        //{
        //    string str = pickDate();
        //    Debug.Log(str);
        //    StartCoroutine(CheckForDateContinue(point));
        //}

        //IEnumerator CheckForDateContinue(string point)
        //{
        //    while(true)
        //    {
        //        string str = GetReturnedDate();
        //        if (str != "" && str != null)
        //        {
        //            if (point.Equals("1"))
        //                StartDate.text = str;
        //            else
        //                EndDate.text = str;
        //            yield break;
        //        }
        //        else
        //            yield return null;
        //    }
        //}
        public async void closePopup()
        {
            Time.timeScale = 1f;
            if (publicHolder != null)
            {
                await System.Threading.Tasks.Task.Delay(300);
                publicHolder.SetActive(false);
                //BackgroundBlur.SetActive(false);
            }
        }

        public void ShowErr()
        {
            OpenPopup("Err");
            //Time.timeScale = 0f;
        }
        public void Close()
        {
            closePopup();
        }

    }
}