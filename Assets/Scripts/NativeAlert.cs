using UnityEngine;

namespace TwelveCards
{
    public class NativeAlert
    {
        public static void Show(string title, string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            AndroidJavaObject builder = new AndroidJavaObject(
                "android.app.AlertDialog$Builder",
                activity
            );

            builder.Call<AndroidJavaObject>("setTitle", title);
            builder.Call<AndroidJavaObject>("setMessage", message);
            builder.Call<AndroidJavaObject>("setPositiveButton", "OK", null);
            builder.Call<AndroidJavaObject>("show");
        }));
#endif
        }
    }
}