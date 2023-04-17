// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Shaman;

//192077 - Wind Rush Totem
//97285 - NPC ID
[CreatureScript(97285)]
public class NPCWindRushTotem : ScriptedAI
{
    public NPCWindRushTotem(Creature creature) : base(creature) { }

    public override void Reset()
    {
        var time = TimeSpan.FromSeconds(1);

        Me.Events.AddRepeatEventAtOffset(() =>
                                         {
                                             Me.SpellFactory.CastSpell(Me, TotemSpells.TOTEM_WIND_RUSH_EFFECT, true);

                                             return time;
                                         },
                                         time);
    }
}