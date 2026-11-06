using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SingleChance
{
    public class DetectNumber : MonoBehaviour
    {
        [HideInInspector]
        public GameObject LastObject = null;
        [HideInInspector]
        public bool StopDetecting = false;
        public AudioSource SpinWheel;
        void OnTriggerStay2D(Collider2D col)
        {
            //Debug.Log(col.gameObject.name);
            if (!StopDetecting)
                LastObject = col.gameObject;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (GetPoints.Is_Spinning)
                SpinWheel.Play();
        }
    }
}