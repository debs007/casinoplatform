using System.Collections;
using System.Collections.Generic;
using Commands;
using UnityEngine;
using UnityEngine.UIElements;
using ViewModel;
using UniRx;
using System;

namespace Components
{
    public class LastTableNumberDisplay : MonoBehaviour
    {
        public CharacterTable characterTable;
        public GameRoullete gameRoullete;

        public GameObject[] anchorNumbers;

        public GameObject numberContainer;
        public Transform instanceContainer;
        
        private int _onScreen = 0;

        public void Start()
        {
            gameRoullete.OnNumber
                .Subscribe(LastNumberDisplay)
                .AddTo(this);

            Reset();
        }

        private void LastNumberDisplay(int value)
        {       
            // full: the oldest number leaves, the others move one place (it used to clear the whole strip)
            if(_onScreen >= (NumberDisplayConfig._smallerPosition + NumberDisplayConfig._biggerPosition))
            {
                RemoveOldest();
            } 

            RepositionLastNumbers();
            AddNumber(value);   
            _onScreen++;
        }

        /// <summary>Casino roulette: shows the server's last results, newest first.</summary>
        public void ShowHistory(int[] newestFirst)
        {
            Reset();
            int max = NumberDisplayConfig._smallerPosition + NumberDisplayConfig._biggerPosition;
            for (int i = Mathf.Min(newestFirst.Length, max) - 1; i >= 0; i--)
            {
                LastNumberDisplay(newestFirst[i]);
            }
        }

        private void RemoveOldest()
        {
            LastNumber oldest = null;
            foreach (LastNumber n in instanceContainer.GetComponentsInChildren<LastNumber>())
            {
                if (oldest == null || n.currentPosition > oldest.currentPosition) oldest = n;
            }
            if (oldest == null)
                return;
            // out of the container now: Destroy only happens at the end of the frame
            oldest.transform.SetParent(null);
            Destroy(oldest.gameObject);
            _onScreen--;
        }

        private void Reset()
        {
            _onScreen = 0;
            characterTable.currentNumbers.Clear();
            List<Transform> shown = new List<Transform>();
            foreach(Transform go in instanceContainer.transform)
            {
                shown.Add(go);
            }
            foreach(Transform go in shown)
            {
                // out of the container now: Destroy only happens at the end of the frame
                go.SetParent(null);
                Destroy(go.gameObject);
            }
        }

        private void RepositionLastNumbers()
        {
            if(_onScreen == 0)
                return;
            
            LastNumber[] numbersOnScreen = instanceContainer.GetComponentsInChildren<LastNumber>();
            foreach(LastNumber t in numbersOnScreen)
            {
                t.currentPosition++;
                t.gameObject.transform.position = FindNumberPosition(t.currentPosition);
                t.transform.localScale = t.currentPosition >= NumberDisplayConfig._biggerPosition ? NumberDisplayConfig._smallerScale : NumberDisplayConfig._biggerScale;
            }
        }

        public void AddNumber(int value)
        {
            GameObject num = GetNumberObject(value);           
            LastNumber lastNumber = num.AddComponent<LastNumber>();
            lastNumber.currentPosition = 0;
            num.transform.SetParent(instanceContainer.transform);
            num.name = "last_" + _onScreen.ToString();
        }
        
        private GameObject GetNumberObject(int value)
        {
            GameObject num = Instantiate(numberContainer.transform.GetChild(value).gameObject);
            num.transform.localPosition = FindNumberPosition(0);
            num.GetComponent<LeanTweenScale>()._scaleXYZ = NumberDisplayConfig._biggerScale;
            return num;
        }

        private Vector3 FindNumberPosition(int onScreen)
        {
            Vector3 pos = anchorNumbers[onScreen].GetComponent<SpriteRenderer>().bounds.center;
            return pos;
        }
    }

    public static class NumberDisplayConfig
    {
        public const int _biggerPosition = 5;
        public const int _smallerPosition = 7;
        public readonly static Vector3 _smallerScale = new Vector3(0.5f,0.5f,0.5f);
        public readonly static Vector3 _biggerScale = new Vector3(0.75f, 0.75f, 0.75f);
    }
}