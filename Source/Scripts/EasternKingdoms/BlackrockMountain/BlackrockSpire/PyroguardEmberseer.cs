// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.PyroguardEmberseer;

internal struct SpellIds
{
    public const uint ENCAGED_EMBERSEER = 15282;         // Self on spawn
    public const uint FIRE_SHIELD_TRIGGER = 13377;       // Self on spawn missing from 335 dbc triggers public const uint FireShield every 3 sec
    public const uint FIRE_SHIELD = 13376;               // Triggered by public const uint FireShieldTrigger
    public const uint FREEZE_ANIM = 16245;               // Self on event start
    public const uint EMBERSEER_GROWING = 16048;         // Self on event start
    public const uint EMBERSEER_GROWING_TRIGGER = 16049; // Triggered by public const uint EmberseerGrowing
    public const uint EMBERSEER_FULL_STRENGTH = 16047;   // Emberseer Full Strength
    public const uint FIRENOVA = 23462;                  // Combat
    public const uint FLAMEBUFFET = 23341;               // Combat

    public const uint PYROBLAST = 17274; // Combat

    // Blackhand Incarcerator public const uint s
    public const uint ENCAGE_EMBERSEER = 15281; // Emberseer on spawn
    public const uint STRIKE = 15580;           // Combat

    public const uint ENCAGE = 16045; // Combat

    // Cast on player by altar
    public const uint EMBERSEER_OBJECT_VISUAL = 16532;
}

internal struct TextIds
{
    public const uint EMOTE_ONE_STACK = 0;
    public const uint EMOTE_TEN_STACK = 1;
    public const uint EMOTE_FREE_OF_BONDS = 2;
    public const uint YELL_FREE_OF_BONDS = 3;
}

[Script]
internal class BossPyroguardEmberseer : BossAI
{
    public BossPyroguardEmberseer(Creature creature) : base(creature, DataTypes.PYROGAURD_EMBERSEER) { }

    public override void Reset()
    {
        Me.SetUnitFlag(UnitFlags.Uninteractible);
        Me.SetImmuneToPC(true);
        Scheduler.CancelAll();
        // Apply Auras on spawn and reset
        // DoCast(me, SpellFireShieldTrigger); // Need to find this in old Dbc if possible
        Me.RemoveAura(SpellIds.EMBERSEER_FULL_STRENGTH);
        Me.RemoveAura(SpellIds.EMBERSEER_GROWING);
        Me.RemoveAura(SpellIds.EMBERSEER_GROWING_TRIGGER);

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           task =>
                           {
                               Instance.SetData(DataTypes.BLACKHAND_INCARCERATOR, 1);
                               Instance.SetBossState(DataTypes.PYROGAURD_EMBERSEER, EncounterState.NotStarted);
                           });

        // Hack for missing trigger spell
        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               // #### Spell isn't doing any Damage ??? ####
                               DoCast(Me, SpellIds.FIRE_SHIELD);
                               task.Repeat(TimeSpan.FromSeconds(3));
                           });
    }

    public override void SetData(uint type, uint data)
    {
        switch (data)
        {
            case 1:
                Scheduler.Schedule(TimeSpan.FromSeconds(5),
                                   task =>
                                   {
                                       // As of Patch 3.0.8 only one person needs to channel the altar
                                       var hasAura = false;
                                       var players = Me.Map.Players;

                                       foreach (var player in players)
                                           if (player != null &&
                                               player.HasAura(SpellIds.EMBERSEER_OBJECT_VISUAL))
                                           {
                                               hasAura = true;

                                               break;
                                           }

                                       if (hasAura)
                                       {
                                           task.Schedule(TimeSpan.FromSeconds(1),
                                                         preFlightTask1 =>
                                                         {
                                                             // Set data on all Blackhand Incarcerators
                                                             var creatureList = Me.GetCreatureListWithEntryInGrid(CreaturesIds.BLACKHAND_INCARCERATOR, 35.0f);

                                                             foreach (var creature in creatureList)
                                                                 if (creature)
                                                                 {
                                                                     creature.SetImmuneToAll(false);
                                                                     creature.InterruptSpell(CurrentSpellTypes.Channeled);
                                                                     DoZoneInCombat(creature);
                                                                 }

                                                             Me.RemoveAura(SpellIds.ENCAGED_EMBERSEER);

                                                             preFlightTask1.Schedule(TimeSpan.FromSeconds(32),
                                                                                     preFlightTask2 =>
                                                                                     {
                                                                                         Me.SpellFactory.CastSpell(Me, SpellIds.FREEZE_ANIM);
                                                                                         Me.SpellFactory.CastSpell(Me, SpellIds.EMBERSEER_GROWING);
                                                                                         Talk(TextIds.EMOTE_ONE_STACK);
                                                                                     });
                                                         });

                                           Instance.SetBossState(DataTypes.PYROGAURD_EMBERSEER, EncounterState.InProgress);
                                       }
                                   });

                break;
        }
    }

    public override void JustEngagedWith(Unit who)
    {
        // ### Todo Check combat timing ###
        Scheduler.Schedule(TimeSpan.FromSeconds(6),
                           task =>
                           {
                               DoCast(Me, SpellIds.FIRENOVA);
                               task.Repeat(TimeSpan.FromSeconds(6));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           task =>
                           {
                               DoCast(Me, SpellIds.FLAMEBUFFET);
                               task.Repeat(TimeSpan.FromSeconds(14));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(14),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);

                               if (target)
                                   DoCast(target, SpellIds.PYROBLAST);

                               task.Repeat(TimeSpan.FromSeconds(15));
                           });
    }

    public override void JustDied(Unit killer)
    {
        // Activate all the runes
        UpdateRunes(GameObjectState.Ready);
        // Complete encounter
        Instance.SetBossState(DataTypes.PYROGAURD_EMBERSEER, EncounterState.Done);
    }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        if (spellInfo.Id == SpellIds.ENCAGE_EMBERSEER)
            if (Me.GetAuraCount(SpellIds.ENCAGED_EMBERSEER) == 0)
            {
                Me.SpellFactory.CastSpell(Me, SpellIds.ENCAGED_EMBERSEER);
                Reset();
            }

        if (spellInfo.Id == SpellIds.EMBERSEER_GROWING_TRIGGER)
        {
            if (Me.GetAuraCount(SpellIds.EMBERSEER_GROWING_TRIGGER) == 10)
                Talk(TextIds.EMOTE_TEN_STACK);

            if (Me.GetAuraCount(SpellIds.EMBERSEER_GROWING_TRIGGER) == 20)
            {
                Me.RemoveAura(SpellIds.FREEZE_ANIM);
                Me.SpellFactory.CastSpell(Me, SpellIds.EMBERSEER_FULL_STRENGTH);
                Talk(TextIds.EMOTE_FREE_OF_BONDS);
                Talk(TextIds.YELL_FREE_OF_BONDS);
                Me.RemoveUnitFlag(UnitFlags.Uninteractible);
                Me.SetImmuneToPC(false);
                Scheduler.Schedule(TimeSpan.FromSeconds(2), task => { AttackStart(Me.SelectNearestPlayer(30.0f)); });
            }
        }
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);

        if (!UpdateVictim())
            return;

        DoMeleeAttackIfReady();
    }

    private void UpdateRunes(GameObjectState state)
    {
        // update all runes
        var rune1 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EMBERSEER_RUNE1));

        if (rune1)
            rune1.SetGoState(state);

        var rune2 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EMBERSEER_RUNE2));

        if (rune2)
            rune2.SetGoState(state);

        var rune3 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EMBERSEER_RUNE3));

        if (rune3)
            rune3.SetGoState(state);

        var rune4 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EMBERSEER_RUNE4));

        if (rune4)
            rune4.SetGoState(state);

        var rune5 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EMBERSEER_RUNE5));

        if (rune5)
            rune5.SetGoState(state);

        var rune6 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EMBERSEER_RUNE6));

        if (rune6)
            rune6.SetGoState(state);

        var rune7 = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(GameObjectsIds.EMBERSEER_RUNE7));

        if (rune7)
            rune7.SetGoState(state);
    }
}

[Script]
internal class NPCBlackhandIncarcerator : ScriptedAI
{
    public NPCBlackhandIncarcerator(Creature creature) : base(creature) { }

    public override void JustAppeared()
    {
        DoCast(SpellIds.ENCAGE_EMBERSEER);
    }

    public override void JustEngagedWith(Unit who)
    {
        // Had to do this because CallForHelp will ignore any npcs without Los
        var creatureList = Me.GetCreatureListWithEntryInGrid(CreaturesIds.BLACKHAND_INCARCERATOR, 60.0f);

        foreach (var creature in creatureList)
            if (creature)
                DoZoneInCombat(creature); // GetAI().AttackStart(me.Victim);

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCastVictim(SpellIds.STRIKE, new CastSpellExtraArgs(true));
                               task.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(23));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCast(SelectTarget(SelectTargetMethod.Random, 0, 100, true), SpellIds.ENCAGE, new CastSpellExtraArgs(true));
                               task.Repeat(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(12));
                           });
    }

    public override void JustReachedHome()
    {
        DoCast(SpellIds.ENCAGE_EMBERSEER);

        Me.SetImmuneToAll(true);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}