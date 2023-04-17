// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Monk;

[CreatureScript(63508)]
public class NPCMonkXuen : ScriptedAI
{
    public NPCMonkXuen(Creature creature) : base(creature) { }

    public override void IsSummonedBy(WorldObject unnamedParameter)
    {
        Me.SpellFactory.CastSpell(Me, MonkSpells.XUEN_AURA, true);
    }
}