// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_pvp_trinket : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        var caster = Caster.AsPlayer;

        switch (caster.EffectiveTeam)
        {
            case TeamFaction.Alliance:
                caster.CastSpell(caster, GenericSpellIds.PvpTrinketAlliance, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                break;
            case TeamFaction.Horde:
                caster.CastSpell(caster, GenericSpellIds.PvpTrinketHorde, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                break;
        }
    }
}