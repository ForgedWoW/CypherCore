// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.AscendantLordObsidius;

internal struct SpellIds
{
    public const uint MANA_TAP = 36021;
    public const uint ARCANE_TORRENT = 36022;
    public const uint DOMINATION = 35280;
}

internal struct TextIds
{
    public const uint YELL_AGGRO = 0;
    public const uint YELL_KILL = 1;
    public const uint YELL_SWITCHING_SHADOWS = 2;
    public const uint YELL_DEATH = 3;

    public const uint EMOTE_SWITCHING_SHADOWS = 4;
}

[Script]
internal class BossAscendantLordObsidius : BossAI
{
    public BossAscendantLordObsidius(Creature creature) : base(creature, DataTypes.ASCENDANT_LORD_OBSIDIUS) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           scheduleTasks =>
                           {
                               DoCastVictim(SpellIds.MANA_TAP, new CastSpellExtraArgs(true));
                               scheduleTasks.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(22));
                           });

        Talk(TextIds.YELL_AGGRO);
    }

    public override void KilledUnit(Unit who)
    {
        if (who.IsPlayer)
            Talk(TextIds.YELL_KILL);
    }

    public override void JustDied(Unit killer)
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