// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Vaelastrasz;

internal struct SpellIds
{
    public const uint Essenceofthered = 23513;
    public const uint Flamebreath = 23461;
    public const uint Firenova = 23462;
    public const uint Tailswipe = 15847;
    public const uint Burningadrenaline = 18173; //Cast this one. It's what 3.3.5 Dbm expects.
    public const uint BurningadrenalineExplosion = 23478;
    public const uint Cleave = 19983; //Chain cleave is most likely named something different and contains a dummy effect
}

internal struct TextIds
{
    public const uint SayLine1 = 0;
    public const uint SayLine2 = 1;
    public const uint SayLine3 = 2;
    public const uint SayHalflife = 3;
    public const uint SayKilltarget = 4;

    public const uint GossipId = 6101;
}

[Script]
internal class boss_vaelastrasz : BossAI
{
    private bool HasYelled;
    private ObjectGuid PlayerGUID;

    public boss_vaelastrasz(Creature creature) : base(creature, DataTypes.VaelastrazTheCorrupt)
    {
        Initialize();
        creature.SetNpcFlag(NPCFlags.Gossip);
        creature.Faction = (uint)FactionTemplates.Friendly;
        creature.RemoveUnitFlag(UnitFlags.Uninteractible);
    }

    public override void Reset()
    {
        _Reset();

        Me.SetStandState(UnitStandStateType.Dead);
        Initialize();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);

        DoCast(Me, SpellIds.Essenceofthered);
        Me.SetHealth(Me.CountPctFromMaxHealth(30));
        // now drop Damage requirement to be able to take loot
        Me.ResetPlayerDamageReq();

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.Cleave);
                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCastVictim(SpellIds.Flamebreath);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(14));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.Firenova);
                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(11),
                           task =>
                           {
                               //Only cast if we are behind
                               if (!Me.Location.HasInArc(MathF.PI, Me.Victim.Location))
                                   DoCast(Me.Victim, SpellIds.Tailswipe);

                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               //selects a random Target that isn't the current victim and is a mana user (selects mana users) but not pets
                               //it also ignores targets who have the aura. We don't want to place the debuff on the same Target twice.
                               var target = SelectTarget(SelectTargetMethod.Random, 1, u => { return u && !u.IsPet && u.DisplayPowerType == PowerType.Mana && !u.HasAura(SpellIds.Burningadrenaline); });

                               if (target != null)
                                   Me.CastSpell(target, SpellIds.Burningadrenaline, true);

                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(45),
                           task =>
                           {
                               //Vael has to cast it himself; contrary to the previous commit's comment. Nothing happens otherwise.
                               Me.CastSpell(Me.Victim, SpellIds.Burningadrenaline, true);
                               task.Repeat(TimeSpan.FromSeconds(45));
                           });
    }

    public override void KilledUnit(Unit victim)
    {
        if ((RandomHelper.Rand32() % 5) != 0)
            return;

        Talk(TextIds.SayKilltarget, victim);
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);

        if (!UpdateVictim())
            return;

        if (Me.HasUnitState(UnitState.Casting))
            return;

        // Yell if hp lower than 15%
        if (HealthBelowPct(15) &&
            !HasYelled)
        {
            Talk(TextIds.SayHalflife);
            HasYelled = true;
        }

        DoMeleeAttackIfReady();
    }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        if (menuId == TextIds.GossipId &&
            gossipListId == 0)
        {
            player.CloseGossipMenu();
            BeginSpeech(player);
        }

        return false;
    }

    private void Initialize()
    {
        PlayerGUID.Clear();
        HasYelled = false;
    }

    private void BeginSpeech(Unit target)
    {
        PlayerGUID = target.GUID;
        Me.RemoveNpcFlag(NPCFlags.Gossip);

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               Talk(TextIds.SayLine1);
                               Me.SetStandState(UnitStandStateType.Stand);
                               Me.HandleEmoteCommand(Emote.OneshotTalk);

                               task.Schedule(TimeSpan.FromSeconds(12),
                                             speechTask2 =>
                                             {
                                                 Talk(TextIds.SayLine2);
                                                 Me.HandleEmoteCommand(Emote.OneshotTalk);

                                                 speechTask2.Schedule(TimeSpan.FromSeconds(12),
                                                                      speechTask3 =>
                                                                      {
                                                                          Talk(TextIds.SayLine3);
                                                                          Me.HandleEmoteCommand(Emote.OneshotTalk);

                                                                          speechTask3.Schedule(TimeSpan.FromSeconds(16),
                                                                                               speechTask4 =>
                                                                                               {
                                                                                                   Me.Faction = (uint)FactionTemplates.DragonflightBlack;
                                                                                                   var player = Global.ObjAccessor.GetPlayer(Me, PlayerGUID);

                                                                                                   if (player)
                                                                                                       AttackStart(player);
                                                                                               });
                                                                      });
                                             });
                           });
    }
}

//Need to define an aurascript for EventBurningadrenaline's death effect.
[Script] // 18173 - Burning Adrenaline
internal class spell_vael_burning_adrenaline : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnAuraRemoveHandler, 2, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void OnAuraRemoveHandler(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        //The tooltip says the on death the AoE occurs. According to information: http://qaliaresponse.stage.lithium.com/t5/WoW-Mayhem/Surviving-Burning-Adrenaline-For-tanks/td-p/48609
        //Burning Adrenaline can be survived therefore Blizzard's implementation was an AoE bomb that went off if you were still alive and dealt
        //Damage to the Target. You don't have to die for it to go off. It can go off whether you live or die.
        Target.CastSpell(Target, SpellIds.BurningadrenalineExplosion, true);
    }
}