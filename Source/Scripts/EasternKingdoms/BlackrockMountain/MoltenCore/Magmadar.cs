// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.MoltenCore.Magmadar;

internal struct SpellIds
{
    public const uint FRENZY = 19451;
    public const uint MAGMA_SPIT = 19449;
    public const uint PANIC = 19408;
    public const uint LAVA_BOMB = 19428;
}

internal struct TextIds
{
    public const uint EMOTE_FRENZY = 0;
}

[Script]
internal class BossMagmadar : BossAI
{
    public BossMagmadar(Creature creature) : base(creature, DataTypes.MAGMADAR) { }

    public override void Reset()
    {
        base.Reset();
        DoCast(Me, SpellIds.MAGMA_SPIT, new CastSpellExtraArgs(true));
    }

    public override void JustEngagedWith(Unit victim)
    {
        base.JustEngagedWith(victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           task =>
                           {
                               Talk(TextIds.EMOTE_FRENZY);
                               DoCast(Me, SpellIds.FRENZY);
                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.PANIC);
                               task.Repeat(TimeSpan.FromSeconds(35));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(12),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true, true, -(int)SpellIds.LAVA_BOMB);

                               if (target)
                                   DoCast(target, SpellIds.LAVA_BOMB);

                               task.Repeat(TimeSpan.FromSeconds(12));
                           });
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}