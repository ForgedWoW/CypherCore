// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenPVPTrinket : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster.AsPlayer;

        switch (caster.EffectiveTeam)
        {
            case TeamFaction.Alliance:
                caster.SpellFactory.CastSpell(caster, GenericSpellIds.PVP_TRINKET_ALLIANCE, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                break;
            case TeamFaction.Horde:
                caster.SpellFactory.CastSpell(caster, GenericSpellIds.PVP_TRINKET_HORDE, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                break;
        }
    }
}