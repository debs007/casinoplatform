using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TwelveCards
{
    public class Interchanger : MonoBehaviour
    {

        public static Interchanger Instance;
        private void Awake()
        {
            Instance = this;
        }

        public Image card;
        public Image symbol;

        private void OnEnable()
        {
            StartCoroutine(AutoChange());
        }

        IEnumerator AutoChange()
        {
            while (true)
            {
                string cr = Constants.cards[Random.Range(0, Constants.cards.Length)];
                string sym = Constants.symbol[Random.Range(0, Constants.symbol.Length)];
                card.sprite = ResultView.CardGet[cr.Split(' ')[0]];
                symbol.sprite = ResultView.symbolGet[sym];
                yield return new WaitForSeconds(0.25f);
            }
        }
    }
}