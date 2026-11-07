using System.Collections;
using System.Collections.Generic;
using Commands;
using Components;
using UnityEngine;
using ViewModel;

namespace Components
{
    public class GameUndoInput : MonoBehaviour
    {
        public CharacterTable characterTable;
        public GameCmdFactory gameCmdFactory;
        // Casino roulette: only chips not sent yet can be taken back
        public void OnClick() 
        {
            if (roulette.RouletteGame.Instance != null)
                roulette.RouletteGame.Instance.UndoLastChip();
        }
    }
}
