using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace TwelveCards
{
    public class DetectNumber : MonoBehaviour
    {

        public GameObject LastObject = null;
        public GameObject LastObject2 = null;

        public bool StopDetecting = false;
        public bool StopDetecting2 = false;
        public AudioSource SpinWheel;

        public bool firstDetector;

        private void Awake()
        {
            if (firstDetector)
            {
                StopDetecting2 = true;
            }
            else
            {
                StopDetecting = true;
            }
        }
        void OnTriggerEnter2D(Collider2D col)
        {
            //Debug.Log(col.gameObject.name);
            if (!StopDetecting && col.gameObject.tag != "second" && firstDetector)
                LastObject = col.gameObject;
            else if (!StopDetecting2 && col.gameObject.tag == "second" && !firstDetector)
            {
                LastObject2 = col.gameObject;
            }

            //if (GetPoints.Is_Spinning && firstDetector)
            //    SpinWheel.Play();
        }


    }
}