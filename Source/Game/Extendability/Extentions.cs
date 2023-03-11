using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;

namespace System
{
    public static class Extentions
    {
        public static HashSet<int> ExplodeHash(this uint hash)
        {
            var newSet = new HashSet<int>();

            foreach (var i in SpellConst.MaxEffects)
                if ((hash & (1 << i)) != 0)
                    newSet.Add(i);

            return newSet;
        }

        public static HashSet<int> ExplodeHash(this int hash)
        {
            var newSet = new HashSet<int>();

            foreach (var i in SpellConst.MaxEffects)
                if ((hash & (1 << i)) != 0)
                    newSet.Add(i);
                else
                    break;

            return newSet;
        }
    }
}
