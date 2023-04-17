// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;

namespace Scripts.World;

[Script]
internal class XPBoostPlayerScript : ScriptObjectAutoAdd, IPlayerOnGiveXP
{
    public XPBoostPlayerScript() : base("xp_boost_PlayerScript") { }

    public void OnGiveXP(Player player, ref uint amount, Unit victim)
    {
        if (IsXPBoostActive())
            amount *= (uint)GetDefaultValue("XP.Boost.Rate", 2.0f);
    }

    private bool IsXPBoostActive()
    {
        var time = GameTime.GetGameTime();
        var localTm = Time.UnixTimeToDateTime(time);
        var weekdayMaskBoosted = GetDefaultValue("XP.Boost.Daymask", 0);
        var weekdayMask = 1u << localTm.Day;
        var currentDayBoosted = (weekdayMask & weekdayMaskBoosted) != 0;

        return currentDayBoosted;
    }
}