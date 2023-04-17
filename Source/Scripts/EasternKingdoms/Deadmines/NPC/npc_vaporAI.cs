// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Scripts.EasternKingdoms.Deadmines.Bosses;
using static Scripts.EasternKingdoms.Deadmines.Bosses.BossAdmiralRipsnarl;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(47714)]
public class NPCVapor : ScriptedAI
{
    private readonly InstanceScript _instance;

    private bool _form1;
    private bool _form2;
    private bool _form3;

    public NPCVapor(Creature creature) : base(creature)
    {
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Events.Reset();
        _form1 = false;
        _form2 = false;
        _form3 = false;
    }

    public override void JustEnteredCombat(Unit who)
    {
        if (!Me)
            return;

        if (IsHeroic())
            Me.AddAura(ESpells.CONDENSATION, Me);
    }

    public override void JustDied(Unit killer)
    {
        var ripsnarl = Me.FindNearestCreature(DmCreatures.NPC_ADMIRAL_RIPSNARL, 250, true);

        if (ripsnarl != null)
        {
            var pAI = (BossAdmiralRipsnarl)ripsnarl.AI;

            if (pAI != null)
                pAI.VaporsKilled();
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Events.Update(diff);

        if (Me.HasAura(ESpells.CONDENSE) && !_form1)
        {
            Events.ScheduleEvent(VaporEvents.EVENT_CONDENSING_VAPOR, TimeSpan.FromMilliseconds(2000));
            _form1 = true;
        }
        else if (Me.HasAura(ESpells.CONDENSE_2) && !_form2)
        {
            Me.SetDisplayId(25654);
            Events.CancelEvent(VaporEvents.EVENT_CONDENSING_VAPOR);
            Events.ScheduleEvent(VaporEvents.EVENT_SWIRLING_VAPOR, TimeSpan.FromMilliseconds(2000));
            _form2 = true;
        }
        else if (Me.HasAura(ESpells.CONDENSE_3) && !_form3)
        {
            Me.SetDisplayId(36455);
            Events.CancelEvent(VaporEvents.EVENT_SWIRLING_VAPOR);
            Events.ScheduleEvent(VaporEvents.EVENT_FREEZING_VAPOR, TimeSpan.FromMilliseconds(2000));
            _form3 = true;
        }

        uint eventId;

        while ((eventId = Events.ExecuteEvent()) != 0)
            switch (eventId)
            {
                case VaporEvents.EVENT_CONDENSING_VAPOR:
                    DoCastVictim(ESpells.CONDENSING_VAPOR);
                    Events.ScheduleEvent(VaporEvents.EVENT_SWIRLING_VAPOR, TimeSpan.FromMilliseconds(3500));

                    break;
                case VaporEvents.EVENT_SWIRLING_VAPOR:
                    DoCastVictim(ESpells.SWIRLING_VAPOR);
                    Events.ScheduleEvent(VaporEvents.EVENT_SWIRLING_VAPOR, TimeSpan.FromMilliseconds(3500));

                    break;
                case VaporEvents.EVENT_FREEZING_VAPOR:
                    DoCastVictim(ESpells.FREEZING_VAPOR);
                    Events.ScheduleEvent(VaporEvents.EVENT_COALESCE, TimeSpan.FromMilliseconds(5000));

                    break;
                case VaporEvents.EVENT_COALESCE:
                    DoCastVictim(ESpells.COALESCE);

                    break;
            }

        DoMeleeAttackIfReady();
    }

    public struct VaporEvents
    {
        public const uint EVENT_CONDENSING_VAPOR = 1;
        public const uint EVENT_SWIRLING_VAPOR = 2;
        public const uint EVENT_FREEZING_VAPOR = 3;
        public const uint EVENT_COALESCE = 4;
    }
}