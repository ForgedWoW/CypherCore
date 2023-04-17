// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Shaman;

[CreatureScript(78001)]
//104818 - Ancestral Protection Totem
public class NPCAncestralProtectionTotem : ScriptedAI
{
    public NPCAncestralProtectionTotem(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Me.SpellFactory.CastSpell(Me, TotemSpells.TOTEM_ANCESTRAL_PROTECTION_AT, true);
    }
}