// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Serilog;

namespace Framework.Threading;

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
            Log.Logger.Error(ex, "");
        }
    }
}