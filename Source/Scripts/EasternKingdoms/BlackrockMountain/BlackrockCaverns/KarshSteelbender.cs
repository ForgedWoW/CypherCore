// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.KarshSteelbender;

internal struct SpellIds
{
    public const uint CLEAVE = 15284;
    public const uint QUICKSILVER_ARMOR = 75842;
    public const uint SUPERHEATED_QUICKSILVER_ARMOR = 75846;
}

internal struct TextIds
{
    public const uint YELL_AGGRO = 0;
    public const uint YELL_KILL = 1;
    public const uint YELL_QUICKSILVER_ARMOR = 2;
    public const uint YELL_DEATH = 3;

    public const uint EMOTE_QUICKSILVER_ARMOR = 4;
}

[Script]
internal class BossKarshSteelbender : BossAI
{
    public BossKarshSteelbender(Creature creature) : base(creature, DataTypes.KARSH_STEELBENDER) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.YELL_AGGRO);

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(10));
                           });
    }

    public override void KilledUnit(Unit who)
    {
        if (who.IsPlayer)
            Talk(TextIds.YELL_KILL);
    }

    public override void JustDied(Unit victim)
    {
        _JustDied();
        Talk(TextIds.YELL_DEATH);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}