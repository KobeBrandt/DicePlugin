namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal static class CustomDiceVar
    {
        private static int diceSides = 0;
        public static void setDiceSide(Int32 diceSide) => diceSides = diceSide;
        public static int getDiceSides() => diceSides;
    }
}
