// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Shaman;

//60561
[CreatureScript(60561)]
public class NPCEarthGrabTotem : ScriptedAI
{
    public List<ObjectGuid> AlreadyRooted = new();

    public NPCEarthGrabTotem(Creature creature) : base(creature) { }

    public override void Reset()
    {
        var time = TimeSpan.FromSeconds(2);

        Me.Events.AddRepeatEventAtOffset(() =>
                                         {
                                             var unitList = new List<Unit>();
                                             Me.GetAttackableUnitListInRange(unitList, 10.0f);

                                             foreach (var target in unitList)
                                             {
                                                 if (target.HasAura(TotemSpells.TOTEM_EARTH_GRAB_ROOT_EFFECT))
                                                     continue;

                                                 if (!AlreadyRooted.Contains(target.GUID))
                                                 {
                                                     AlreadyRooted.Add(target.GUID);
                                                     Me.SpellFactory.CastSpell(target, TotemSpells.TOTEM_EARTH_GRAB_ROOT_EFFECT, true);
                                                 }
                                                 else
                                                 {
                                                     Me.SpellFactory.CastSpell(target, TotemSpells.TOTEM_EARTH_GRAB_SLOW_EFFECT, true);
                                                 }
                                             }

                                             return time;
                                         },
                                         time);
    }
}