using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SingleChance
{
    public class HoldingValues : MonoBehaviour
    {
        public Sprite Chnageable;
        [HideInInspector]
        public Sprite Default;
        public int Value;

        void Awake()
        {
            Default = this.GetComponent<Image>().sprite;
        }
    }
}
