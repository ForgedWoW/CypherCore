﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Framework.Constants;

namespace Forged.MapServer.AI.CoreAI;

public class NullCreatureAI : CreatureAI
{
    public NullCreatureAI(Creature creature) : base(creature)
    {
        creature.ReactState = ReactStates.Passive;
    }

    public override void AttackStart(Unit unit)
    { }

    public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
    { }

    public override void JustAppeared()
    { }

    public override void JustEnteredCombat(Unit who)
    { }

    public override void JustStartedThreateningMe(Unit unit)
    { }

    public override void MoveInLineOfSight(Unit unit)
    { }

    public override void OnCharmed(bool isNew)
    { }

    public override void UpdateAI(uint diff)
    { }
}