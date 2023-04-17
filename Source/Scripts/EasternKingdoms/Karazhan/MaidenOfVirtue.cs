// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Karazhan.MaidenOfVirtue;

internal struct SpellIds
{
    public const uint REPENTANCE = 29511;
    public const uint HOLYFIRE = 29522;
    public const uint HOLYWRATH = 32445;
    public const uint HOLYGROUND = 29523;
    public const uint BERSERK = 26662;
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_SLAY = 1;
    public const uint SAY_REPENTANCE = 2;
    public const uint SAY_DEATH = 3;
}

[Script]
internal class BossMaidenOfVirtue : BossAI
{
    public BossMaidenOfVirtue(Creature creature) : base(creature, DataTypes.MAIDEN_OF_VIRTUE) { }

    public override void KilledUnit(Unit victim)
    {
        if (RandomHelper.randChance(50))
            Talk(TextIds.SAY_SLAY);
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_DEATH);
        _JustDied();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.SAY_AGGRO);

        DoCastSelf(SpellIds.HOLYGROUND, new CastSpellExtraArgs(true));

        Scheduler.Schedule(TimeSpan.FromSeconds(33),
                           TimeSpan.FromSeconds(45),
                           task =>
                           {
                               DoCastVictim(SpellIds.REPENTANCE);
                               Talk(TextIds.SAY_REPENTANCE);
                               task.Repeat(TimeSpan.FromSeconds(35));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 50, true);

                               if (target)
                                   DoCast(target, SpellIds.HOLYFIRE);

                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(19));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           TimeSpan.FromSeconds(25),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 80, true);

                               if (target)
                                   DoCast(target, SpellIds.HOLYWRATH);

                               task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
                           });

        Scheduler.Schedule(TimeSpan.FromMinutes(10), task => { DoCastSelf(SpellIds.BERSERK, new CastSpellExtraArgs(true)); });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}