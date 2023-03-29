﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[Script] // 783 - Travel Form (dummy)
internal class spell_dru_travel_form_dummy : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var player = Caster.AsPlayer;

        if (!player)
            return SpellCastResult.CustomError;

        var spellId = (player.HasSpell(DruidSpellIds.FormAquaticPassive) && player.IsInWater) ? DruidSpellIds.FormAquatic : DruidSpellIds.FormStag;

        var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, CastDifficulty);

        return spellInfo.CheckLocation(player.Location.MapId, player.Zone, player.Area, player);
    }
}