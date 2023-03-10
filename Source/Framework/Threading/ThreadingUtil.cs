using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Threading
{
    public static class ThreadingUtil
    {
        public static void ProcessTask(Action a)
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }
        }
    }
}
