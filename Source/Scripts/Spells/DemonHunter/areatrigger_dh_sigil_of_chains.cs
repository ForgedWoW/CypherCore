// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;

namespace Scripts.Spells.DemonHunter;

[Script] // 202138 - Sigil of Chains
internal class AreatriggerDhSigilOfChains : AreaTriggerScript, IAreaTriggerOnRemove
{
    public void OnRemove()
    {
        var caster = At.GetCaster();

        if (caster != null)
        {
            caster.SpellFactory.CastSpell(At.Location, DemonHunterSpells.SIGIL_OF_CHAINS_VISUAL, new CastSpellExtraArgs());
            caster.SpellFactory.CastSpell(At.Location, DemonHunterSpells.SIGIL_OF_CHAINS_TARGET_SELECT, new CastSpellExtraArgs());
        }
    }
}