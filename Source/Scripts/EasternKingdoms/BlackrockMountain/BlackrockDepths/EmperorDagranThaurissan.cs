// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.Draganthaurissan;

internal struct SpellIds
{
    public const uint Handofthaurissan = 17492;
    public const uint Avatarofflame = 15636;
}

internal struct TextIds
{
    public const uint SayAggro = 0;
    public const uint SaySlay = 1;

    public const uint EmoteShaken = 0;
}

[Script]
internal class boss_draganthaurissan : ScriptedAI
{
    private readonly InstanceScript _instance;

    public boss_draganthaurissan(Creature creature) : base(creature)
    {
        _instance = Me.InstanceScript;
    }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SayAggro);
        Me.CallForHelp(166.0f);

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0);

                               if (target)
                                   DoCast(target, SpellIds.Handofthaurissan);

                               task.Repeat(TimeSpan.FromSeconds(5));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(25),
                           task =>
                           {
                               DoCastVictim(SpellIds.Avatarofflame);
                               task.Repeat(TimeSpan.FromSeconds(18));
                           });
    }

    public override void KilledUnit(Unit who)
    {
        if (who.IsPlayer)
            Talk(TextIds.SaySlay);
    }

    public override void JustDied(Unit killer)
    {
        var moira = ObjectAccessor.GetCreature(Me, _instance.GetGuidData(DataTypes.DataMoira));

        if (moira)
        {
            moira.AI.EnterEvadeMode();
            moira.Faction = (uint)FactionTemplates.Friendly;
            moira.AI.Talk(TextIds.EmoteShaken);
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}