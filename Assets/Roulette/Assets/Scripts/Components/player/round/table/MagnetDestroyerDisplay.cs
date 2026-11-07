using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViewModel;
using UniRx;
using Commands;

namespace Components
{
    public class MagnetDestroyerDisplay : MonoBehaviour
    {
        public CharacterTable characterTable;
        public GameCmdFactory gameCmdFactory;
        public GameObject magnetDestroyer;
        public float magnetTime;

        void Start()
        {
            characterTable.OnRound
                .Subscribe(MagnetDestroyer)
                .AddTo(this);
        }

        public void MagnetDestroyer(bool isRound)
        {
            if(isRound)
                return;

            // Casino roulette: RouletteGame sweeps the table itself, before the next round opens
            if (roulette.RouletteGame.Instance != null)
                return;

            gameCmdFactory.ResetTableTurn(this, characterTable, 10).Execute();  
        }
    }
}
