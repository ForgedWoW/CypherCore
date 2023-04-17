// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.Beauty;

internal struct SpellIds
{
    public const uint TERRIFYING_ROAR = 76028; // Not yet Implemented
    public const uint BERSERKER_CHARGE = 76030;
    public const uint MAGMA_SPIT = 76031;
    public const uint FLAMEBREAK = 76032;
    public const uint BERSERK = 82395; // Not yet Implemented
}

internal struct SoundIds
{
    public const uint AGGRO = 18559;
    public const uint DEATH = 18563;
}

[Script]
internal class BossBeauty : BossAI
{
    public BossBeauty(Creature creature) : base(creature, DataTypes.BEAUTY) { }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(7),
                           TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCast(SelectTarget(SelectTargetMethod.Random, 0, 100, true), SpellIds.MAGMA_SPIT, new CastSpellExtraArgs(true));
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(16),
                           TimeSpan.FromSeconds(19),
                           task =>
                           {
                               DoCast(SelectTarget(SelectTargetMethod.Random, 0, 100, true), SpellIds.BERSERKER_CHARGE, new CastSpellExtraArgs(true));
                               task.Repeat();
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(18),
                           TimeSpan.FromSeconds(22),
                           task =>
                           {
                               DoCast(Me, SpellIds.FLAMEBREAK);
                               task.Repeat();
                           });

        DoPlaySoundToSet(Me, SoundIds.AGGRO);
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        DoPlaySoundToSet(Me, SoundIds.DEATH);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}