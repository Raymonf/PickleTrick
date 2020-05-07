using System;
using System.Collections.Generic;
using System.Text;

namespace PickleTrick.Core.Common
{
    public class Constants
    {
        public static Encoding Encoding = Encoding.GetEncoding(949);

        public static Dictionary<int, int> CharacterTypes = new Dictionary<int, int>()
        {
            { 1, 0 },
            { 2, 0 },
            { 9, 0 },

            { 3, 1 },
            { 4, 1 },

            { 5, 2 },
            { 6, 2 },

            { 7, 3 },
            { 8, 3 },
        };
    }
}
