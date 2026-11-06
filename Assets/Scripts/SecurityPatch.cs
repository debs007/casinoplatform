using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TwelveCards
{
    public class SecurityPatch : MonoBehaviour
    {
        public GameObject SecurityObject;
        public Image SecurityFiller;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            //PlayerPrefs.DeleteKey("Cluster");

            if (!PlayerPrefs.HasKey("Cluster"))
            {
                Debug.LogError("Adding security");
                SecurityObject.SetActive(true);
                StartCoroutine(FillSecurityPatch());
            }
            else
            {
                Debug.LogError("Already added");
            }
        }

        IEnumerator FillSecurityPatch()
        {
            while (SecurityFiller.fillAmount < 1f)
            {
                float rand = Random.Range(.02f, .04f);
                SecurityFiller.fillAmount += rand;
                yield return new WaitForSeconds(Random.Range(1f, 3f));
            }
            PlayerPrefs.SetString("Cluster", "2.0");
            SecurityObject.SetActive(false);
        }
    }
}