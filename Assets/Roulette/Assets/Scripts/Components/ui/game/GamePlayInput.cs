using System.Collections;
using System.Collections.Generic;
using Commands;
using UnityEngine;
using ViewModel;

namespace Components
{
    public class GamePlayInput : MonoBehaviour
    {
        public CharacterTable chartacterTable;
        public GameRoullete gameRoullete;
        public GameCmdFactory gameCmdFactory;
        
        // Casino roulette: this is the Bet button. The wheel spins by itself on the server's clock.
        public void OnClick()
        {
            if (roulette.RouletteGame.Instance != null)
                roulette.RouletteGame.Instance.PlaceBet();
            else
                Debug.LogWarning("RouletteGame is missing from the scene");
        }
    }
}
