// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair.Vaelastrasz;

internal struct SpellIds
{
    public const uint ESSENCEOFTHERED = 23513;
    public const uint FLAMEBREATH = 23461;
    public const uint FIRENOVA = 23462;
    public const uint TAILSWIPE = 15847;
    public const uint BURNINGADRENALINE = 18173; //Cast this one. It's what 3.3.5 Dbm expects.
    public const uint BURNINGADRENALINE_EXPLOSION = 23478;
    public const uint CLEAVE = 19983; //Chain cleave is most likely named something different and contains a dummy effect
}

internal struct TextIds
{
    public const uint SAY_LINE1 = 0;
    public const uint SAY_LINE2 = 1;
    public const uint SAY_LINE3 = 2;
    public const uint SAY_HALFLIFE = 3;
    public const uint SAY_KILLTARGET = 4;

    public const uint GOSSIP_ID = 6101;
}

[Script]
internal class BossVaelastrasz : BossAI
{
    private bool _hasYelled;
    private ObjectGuid _playerGUID;

    public BossVaelastrasz(Creature creature) : base(creature, DataTypes.VAELASTRAZ_THE_CORRUPT)
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

        DoCast(Me, SpellIds.ESSENCEOFTHERED);
        Me.SetHealth(Me.CountPctFromMaxHealth(30));
        // now drop Damage requirement to be able to take loot
        Me.ResetPlayerDamageReq();

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               DoCastVictim(SpellIds.FLAMEBREATH);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(14));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(20),
                           task =>
                           {
                               DoCastVictim(SpellIds.FIRENOVA);
                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(11),
                           task =>
                           {
                               //Only cast if we are behind
                               if (!Me.Location.HasInArc(MathF.PI, Me.Victim.Location))
                                   DoCast(Me.Victim, SpellIds.TAILSWIPE);

                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(15),
                           task =>
                           {
                               //selects a random Target that isn't the current victim and is a mana user (selects mana users) but not pets
                               //it also ignores targets who have the aura. We don't want to place the debuff on the same Target twice.
                               var target = SelectTarget(SelectTargetMethod.Random, 1, u => { return u && !u.IsPet && u.DisplayPowerType == PowerType.Mana && !u.HasAura(SpellIds.BURNINGADRENALINE); });

                               if (target != null)
                                   Me.SpellFactory.CastSpell(target, SpellIds.BURNINGADRENALINE, true);

                               task.Repeat(TimeSpan.FromSeconds(15));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(45),
                           task =>
                           {
                               //Vael has to cast it himself; contrary to the previous commit's comment. Nothing happens otherwise.
                               Me.SpellFactory.CastSpell(Me.Victim, SpellIds.BURNINGADRENALINE, true);
                               task.Repeat(TimeSpan.FromSeconds(45));
                           });
    }

    public override void KilledUnit(Unit victim)
    {
        if ((RandomHelper.Rand32() % 5) != 0)
            return;

        Talk(TextIds.SAY_KILLTARGET, victim);
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
            !_hasYelled)
        {
            Talk(TextIds.SAY_HALFLIFE);
            _hasYelled = true;
        }

        DoMeleeAttackIfReady();
    }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        if (menuId == TextIds.GOSSIP_ID &&
            gossipListId == 0)
        {
            player.CloseGossipMenu();
            BeginSpeech(player);
        }

        return false;
    }

    private void Initialize()
    {
        _playerGUID.Clear();
        _hasYelled = false;
    }

    private void BeginSpeech(Unit target)
    {
        _playerGUID = target.GUID;
        Me.RemoveNpcFlag(NPCFlags.Gossip);

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               Talk(TextIds.SAY_LINE1);
                               Me.SetStandState(UnitStandStateType.Stand);
                               Me.HandleEmoteCommand(Emote.OneshotTalk);

                               task.Schedule(TimeSpan.FromSeconds(12),
                                             speechTask2 =>
                                             {
                                                 Talk(TextIds.SAY_LINE2);
                                                 Me.HandleEmoteCommand(Emote.OneshotTalk);

                                                 speechTask2.Schedule(TimeSpan.FromSeconds(12),
                                                                      speechTask3 =>
                                                                      {
                                                                          Talk(TextIds.SAY_LINE3);
                                                                          Me.HandleEmoteCommand(Emote.OneshotTalk);

                                                                          speechTask3.Schedule(TimeSpan.FromSeconds(16),
                                                                                               speechTask4 =>
                                                                                               {
                                                                                                   Me.Faction = (uint)FactionTemplates.DragonflightBlack;
                                                                                                   var player = Global.ObjAccessor.GetPlayer(Me, _playerGUID);

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
internal class SpellVaelBurningAdrenaline : AuraScript, IHasAuraEffects
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
        Target.SpellFactory.CastSpell(Target, SpellIds.BURNINGADRENALINE_EXPLOSION, true);
    }
}