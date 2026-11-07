using System.Collections;
using System.Collections.Generic;
using Commands;
using UnityEngine;
using ViewModel;

namespace Components
{
    public class GameDeleteInput : MonoBehaviour
    {
        public CharacterTable characterTable;
        public GameCmdFactory gameCmdFactory;
        public MagnetDestroyerDisplay magnetDestroyerDisplay;

        // Casino roulette: removes the chips not sent yet (placed bets stay)
        public void OnClick()
        {
            if (roulette.RouletteGame.Instance != null)
                roulette.RouletteGame.Instance.ClearUnsentChips();
        }
    }
}
