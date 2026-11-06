using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SingleChance
{
    public class Rotator : MonoBehaviour
    {
        [Range(0f, 100f)]
        public float _speed = 20f;

        // Update is called once per frame
        void Update()
        {
            this.transform.Rotate(new Vector3(0f, 0f, 1f) * _speed * Time.deltaTime);
        }
    }
}
