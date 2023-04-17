// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns.RomoggBonecrusher;

internal struct SpellIds
{
    public const uint CALL_FOR_HELP = 82137; // Needs Scripting
    public const uint CHAINS_OF_WOE = 75539;
    public const uint QUAKE = 75272;
    public const uint SKULLCRACKER = 75543;
    public const uint WOUNDING_STRIKE = 75571;
}

internal struct TextIds
{
    public const uint YELL_AGGRO = 0;
    public const uint YELL_KILL = 1;
    public const uint YELL_SKULLCRACKER = 2;
    public const uint YELL_DEATH = 3;

    public const uint EMOTE_CALL_FOR_HELP = 4;
    public const uint EMOTE_SKULLCRACKER = 5;
}

internal struct MiscConst
{
    public const uint TYPE_RAZ = 1;
    public const uint DATA_ROMOGG_DEAD = 1;
    public static Position SummonPos = new(249.2639f, 949.1614f, 191.7866f, 3.141593f);
}

[Script]
internal class BossRomoggBonecrusher : BossAI
{
    public BossRomoggBonecrusher(Creature creature) : base(creature, DataTypes.ROMOGG_BONECRUSHER)
    {
        Me.SummonCreature(CreatureIds.RAZ_THE_CRAZED, MiscConst.SummonPos, TempSummonType.ManualDespawn, TimeSpan.FromSeconds(200));
    }

    public override void Reset()
    {
        _Reset();
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        Talk(TextIds.YELL_DEATH);

        var raz = Instance.GetCreature(DataTypes.RAZ_THE_CRAZED);

        if (raz)
            raz.AI.SetData(MiscConst.TYPE_RAZ, MiscConst.DATA_ROMOGG_DEAD);
    }

    public override void KilledUnit(Unit who)
    {
        if (who.IsPlayer)
            Talk(TextIds.YELL_KILL);
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        Scheduler.Schedule(TimeSpan.FromSeconds(22),
                           TimeSpan.FromSeconds(32),
                           task =>
                           {
                               Talk(TextIds.YELL_SKULLCRACKER);
                               DoCast(Me, SpellIds.CHAINS_OF_WOE);
                               task.Repeat(TimeSpan.FromSeconds(22), TimeSpan.FromSeconds(32));

                               Scheduler.Schedule(TimeSpan.FromSeconds(3),
                                                  skullCrackerTask =>
                                                  {
                                                      Talk(TextIds.EMOTE_SKULLCRACKER);
                                                      DoCast(Me, SpellIds.SKULLCRACKER);
                                                  });
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(26),
                           TimeSpan.FromSeconds(32),
                           task =>
                           {
                               DoCastVictim(SpellIds.WOUNDING_STRIKE, new CastSpellExtraArgs(true));
                               task.Repeat(TimeSpan.FromSeconds(26), TimeSpan.FromSeconds(32));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(45),
                           task =>
                           {
                               DoCast(Me, SpellIds.QUAKE);
                               task.Repeat(TimeSpan.FromSeconds(32), TimeSpan.FromSeconds(40));
                           });

        Talk(TextIds.YELL_AGGRO);
        Talk(TextIds.EMOTE_CALL_FOR_HELP);
        DoCast(Me, SpellIds.CALL_FOR_HELP);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}