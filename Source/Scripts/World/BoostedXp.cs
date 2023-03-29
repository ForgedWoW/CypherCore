// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.World;

[Script]
internal class xp_boost_PlayerScript : ScriptObjectAutoAdd, IPlayerOnGiveXP
{
    public xp_boost_PlayerScript() : base("xp_boost_PlayerScript") { }

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