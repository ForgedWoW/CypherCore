// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;

namespace Scripts.Spells.DemonHunter;

[Script]
internal class AreatriggerDhSigilOfMisery : AreaTriggerScript, IAreaTriggerOnRemove
{
    public void OnRemove()
    {
        var caster = At.GetCaster();

        caster?.SpellFactory.CastSpell(At.Location, DemonHunterSpells.SIGIL_OF_MISERY_AOE, new CastSpellExtraArgs());
    }
}