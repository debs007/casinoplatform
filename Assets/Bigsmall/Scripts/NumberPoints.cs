using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace bigsmall
{
    /// <summary>
    /// Big Small: the result is the sum of two dice (2-12).
    /// ByNumber holds the bets the SERVER has accepted for the current session, indexed by the number
    /// itself (ByNumber[7] = bet on 7; indexes 0 and 1 are unused).
    /// Chips placed but not yet sent live in GetPoints (the pending slip).
    /// </summary>
    public class NumberPoints
    {
        public const int Min = 2;
        public const int Max = 12;

        // Small = 2-6, Seven = 7, Big = 8-12
        public const int SmallFrom = 2, SmallTo = 6;
        public const int Seven = 7;
        public const int BigFrom = 8, BigTo = 12;

        public static readonly int[] ByNumber = new int[Max + 1];

        public static bool IsValid(int number)
        {
            return number >= Min && number <= Max;
        }

        public static int Total
        {
            get
            {
                int sum = 0;
                for (int n = Min; n <= Max; n++) sum += ByNumber[n];
                return sum;
            }
        }

        public static void Reset()
        {
            for (int i = 0; i < ByNumber.Length; i++) ByNumber[i] = 0;
        }
    }
}
