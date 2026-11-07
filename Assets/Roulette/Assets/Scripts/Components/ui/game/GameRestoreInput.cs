using System.Collections;
using System.Collections.Generic;
using Commands;
using Components;
using UnityEngine;
using ViewModel;

namespace Components
{
    public class GameRestoreInput : MonoBehaviour
    {
        public CharacterTable characterTable;
        public GameCmdFactory gameCmdFactory;
        // Casino roulette: puts last round's bets on the table again (sent with the Bet button)
        public void OnClick() 
        {
            if (roulette.RouletteGame.Instance != null)
                roulette.RouletteGame.Instance.RepeatLastBet();
        }
    }
}
