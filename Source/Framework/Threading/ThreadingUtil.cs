using System;

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
