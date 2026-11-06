using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Threading.Tasks;

namespace TwelveCards
{
    public class ImageSaver : MonoBehaviour
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private string targetUpiId;

        private void OnEnable()
        {
            LoadImage(Constants.userData.qrImage);
        }
        public async void LoadImage(string imageUrl)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);

            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                return;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            targetImage.sprite = sprite;
            targetImage.preserveAspect = true;
        }
        public void SaveImage()
        {
            if (targetImage == null)
            {
                Debug.LogError("Target Image is not assigned.");
                return;
            }

            if (targetImage.sprite == null)
            {
                Debug.LogError("Image does not have a sprite assigned.");
                return;
            }

            Debug.LogError("Trying to save");
            Texture2D sourceTexture = targetImage.sprite.texture;

            // Create a readable copy of the texture
            Texture2D texture = GetReadableTexture(sourceTexture);

            NativeGallery.SaveImageToGallery(
                 texture,
                 "MyApp",
                 $"Image_{System.DateTime.Now:yyyyMMdd_HHmmss}.png",
                 (success, path) =>
                 {
                     Debug.Log(success
                         ? $"Image saved successfully: {path}"
                         : "Failed to save image");
                 }
             );

            //  Debug.Log("Save Permission Result: " + permission);
        }

        public void CopyUpiId()
        {
            GUIUtility.systemCopyBuffer = Constants.userData.qr;
            NativeAlert.Show("UPI copy", "Copied Upi ID");
        }

        public void OpenWhatsApp()
        {
            string phoneNumber = Constants.userData.whatsapp; // Country code + number, no + sign
            string url = $"https://wa.me/{phoneNumber}";

            Application.OpenURL(url);
        }

        private Texture2D GetReadableTexture(Texture2D source)
        {
            RenderTexture rt = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );

            Graphics.Blit(source, rt);

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D readableTexture = new Texture2D(
                source.width,
                source.height,
                TextureFormat.RGBA32,
                false
            );

            readableTexture.ReadPixels(
                new Rect(0, 0, source.width, source.height),
                0,
                0
            );

            readableTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            return readableTexture;
        }
    }
}