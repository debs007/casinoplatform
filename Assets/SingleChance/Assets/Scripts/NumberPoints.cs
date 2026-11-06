using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bets the SERVER has accepted for the current session, per number (0-9).
/// Only changed after a successful updateBet response, or reset when a round ends.
/// Chips the player has placed but not yet sent live in GetPoints (the pending slip).
/// </summary>

namespace SingleChance
{
    public class NumberPoints
    {
        public static readonly int[] ByNumber = new int[10];

        public static int zero { get { return ByNumber[0]; } set { ByNumber[0] = value; } }
        public static int one { get { return ByNumber[1]; } set { ByNumber[1] = value; } }
        public static int two { get { return ByNumber[2]; } set { ByNumber[2] = value; } }
        public static int three { get { return ByNumber[3]; } set { ByNumber[3] = value; } }
        public static int four { get { return ByNumber[4]; } set { ByNumber[4] = value; } }
        public static int five { get { return ByNumber[5]; } set { ByNumber[5] = value; } }
        public static int six { get { return ByNumber[6]; } set { ByNumber[6] = value; } }
        public static int seven { get { return ByNumber[7]; } set { ByNumber[7] = value; } }
        public static int eight { get { return ByNumber[8]; } set { ByNumber[8] = value; } }
        public static int nine { get { return ByNumber[9]; } set { ByNumber[9] = value; } }

        public static int Total
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < ByNumber.Length; i++) sum += ByNumber[i];
                return sum;
            }
        }

        public static void Reset()
        {
            for (int i = 0; i < ByNumber.Length; i++) ByNumber[i] = 0;
        }
    }
}