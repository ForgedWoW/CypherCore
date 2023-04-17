// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49481)]
public class NPCIcycleDm : NullCreatureAI
{
    public uint HitTimer;

    public NPCIcycleDm(Creature creature) : base(creature)
    {
        Me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.NonAttackable | UnitFlags.Pacified);
        Me.ReactState = ReactStates.Passive;
        Me.SetDisplayId(28470);
    }

    public override void Reset()
    {
        HitTimer = 2500;
    }

    public override void UpdateAI(uint diff)
    {
        if (HitTimer <= diff)
        {
            DoCast(Me, 92201);
            DoCast(Me, 62453);
        }
        else
            HitTimer -= diff;
    }
}