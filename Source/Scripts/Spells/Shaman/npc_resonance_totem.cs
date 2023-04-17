// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Shaman;

//NPC ID : 102392
[CreatureScript(102392)]
public class NPCResonanceTotem : ScriptedAI
{
    public NPCResonanceTotem(Creature creature) : base(creature) { }

    public override void Reset()
    {
        var time = TimeSpan.FromSeconds(1);

        Me.Events.AddRepeatEventAtOffset(() =>
                                         {
                                             Me.SpellFactory.CastSpell(Me, TotemSpells.TOTEM_RESONANCE_EFFECT, true);

                                             return time;
                                         },
                                         time);
    }
}