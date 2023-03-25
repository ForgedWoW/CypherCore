// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Serilog
{
    public static class LoggerExtensions
    {
        public static void Error(this ILogger logger, Exception ex)
        {
            logger.Error("{0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace);

            if (ex.InnerException != null)
                logger.Error("Inner Exception: {0}{1}{2}", ex.InnerException.Message, Environment.NewLine, ex.InnerException.StackTrace);
        }
    }
}
