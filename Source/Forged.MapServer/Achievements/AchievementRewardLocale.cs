// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;
using Framework.Constants;

namespace Forged.MapServer.Achievements;

public class AchievementRewardLocale
{
    public StringArray Body = new((int)Locale.Total);
    public StringArray Subject = new((int)Locale.Total);
}