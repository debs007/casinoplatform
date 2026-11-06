using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveCards
{
    public class Reflector : MonoBehaviour
    {
        public ReflectType reflectType;

        public Text[] ReflectTexts;

        private void OnGUI()
        {
            if (reflectType == ReflectType.text)
            {
                foreach (Text txt in ReflectTexts)
                {
                    txt.text = this.GetComponent<Text>().text;
                }
            }
        }
    }

    public enum ReflectType
    {
        text,
        image
    }
}