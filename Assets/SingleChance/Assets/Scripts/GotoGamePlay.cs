using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SingleChance
{
    public class GotoGamePlay : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(GoToMainPlay());
        }

        IEnumerator GoToMainPlay()
        {
            yield return new WaitForSeconds(2f);
            Initiate.Fade("GamePlayScene", Color.black, 2.0f);
        }
    }
}
