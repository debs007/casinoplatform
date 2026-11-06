using UnityEngine;
using UnityEngine.UI;

namespace TwelveCards
{
    public class TransferObjects : MonoBehaviour
    {
        public Text userName;
        public Text Amount;
        public Text DateTime;


        public void SetTransferDetails(string uname, string amount, string dt)
        {
            userName.text = uname;
            this.Amount.text = $"<color=green>+{amount}</color>";
            DateTime.text = $"Date: {dt}";
        }
    }
}
