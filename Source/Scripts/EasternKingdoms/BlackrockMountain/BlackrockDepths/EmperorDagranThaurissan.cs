// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.Draganthaurissan;

internal struct SpellIds
{
    public const uint HANDOFTHAURISSAN = 17492;
    public const uint AVATAROFFLAME = 15636;
}

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_SLAY = 1;

    public const uint EMOTE_SHAKEN = 0;
}

[Script]
internal class BossDraganthaurissan : ScriptedAI
{
    private readonly InstanceScript _instance;

    public BossDraganthaurissan(Creature creature) : base(creature)
    {
        _instance = Me.InstanceScript;
    }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_AGGRO);
        Me.CallForHelp(166.0f);

        Scheduler.Schedule(TimeSpan.FromSeconds(4),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0);

                               if (target)
                                   DoCast(target, SpellIds.HANDOFTHAURISSAN);

                               task.Repeat(TimeSpan.FromSeconds(5));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(25),
                           task =>
                           {
                               DoCastVictim(SpellIds.AVATAROFFLAME);
                               task.Repeat(TimeSpan.FromSeconds(18));
                           });
    }

    public override void KilledUnit(Unit who)
    {
        if (who.IsPlayer)
            Talk(TextIds.SAY_SLAY);
    }

    public override void JustDied(Unit killer)
    {
        var moira = ObjectAccessor.GetCreature(Me, _instance.GetGuidData(DataTypes.DATA_MOIRA));

        if (moira)
        {
            moira.AI.EnterEvadeMode();
            moira.Faction = (uint)FactionTemplates.Friendly;
            moira.AI.Talk(TextIds.EMOTE_SHAKEN);
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}