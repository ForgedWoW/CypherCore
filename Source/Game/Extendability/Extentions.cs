using System.Collections.Generic;

namespace System
{
    public static class Extentions
    {
        public static HashSet<int> ExplodeMask(this uint mask, HashSet<int> maxValues)
        {
            var newSet = new HashSet<int>();

            foreach (var i in maxValues)
                if ((mask & (1 << i)) != 0)
                    newSet.Add(i);

            return newSet;
        }

        public static HashSet<int> ExplodeMask(this uint mask, int maxValue)
        {
            return mask.ExplodeMask(new HashSet<int>().Fill(maxValue));
        }

        public static HashSet<int> ExplodeMask(this int mask, HashSet<int> maxValues)
        {
            var newSet = new HashSet<int>();

            foreach (var i in maxValues)
                if ((mask & (1 << i)) != 0)
                    newSet.Add(i);

            return newSet;
        }

        public static HashSet<int> ExplodeMask(this int mask, int maxValue)
        {
            return mask.ExplodeMask(new HashSet<int>().Fill(maxValue));
        }
    }
}
