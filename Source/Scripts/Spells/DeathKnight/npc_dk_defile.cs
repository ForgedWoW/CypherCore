// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script]
public class NPCDkDefile : ScriptedAI
{
    public NPCDkDefile(Creature creature) : base(creature)
    {
        SetCombatMovement(false);
        Me.ReactState = ReactStates.Passive;
        Me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable);
        Me.AddUnitState(UnitState.Root);
    }

    public override void Reset()
    {
        Me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(11));
    }
}