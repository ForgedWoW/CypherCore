// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// Light's Hammer
// NPC Id - 59738
[CreatureScript(59738)]
public class NPCPalLightsHammer : ScriptedAI
{
    public NPCPalLightsHammer(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Me.SpellFactory.CastSpell(Me, PaladinSpells.LIGHT_HAMMER_COSMETIC, true);
        Me.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible | UnitFlags.RemoveClientControl);
    }
}