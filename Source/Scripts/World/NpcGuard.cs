// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.World.NpcGuard;

internal struct SpellIds
{
    public const uint BanishedShattrathA = 36642;
    public const uint BanishedShattrathS = 36671;
    public const uint BanishTeleport = 36643;
    public const uint Exile = 39533;
}

internal struct TextIds
{
    public const uint SayGuardSilAggro = 0;
}

internal struct CreatureIds
{
    public const uint CenarionHoldInfantry = 15184;
    public const uint StormwindCityGuard = 68;
    public const uint StormwindCityPatroller = 1976;
    public const uint OrgrimmarGrunt = 3296;
    public const uint AldorVindicator = 18549;
}

[Script]
internal class npc_guard_generic : GuardAI
{
    private readonly TaskScheduler _combatScheduler;

    public npc_guard_generic(Creature creature) : base(creature)
    {
        Scheduler.SetValidator(() => !Me.HasUnitState(UnitState.Casting) && !Me.IsInEvadeMode && Me.IsAlive);
        _combatScheduler = new TaskScheduler();
        _combatScheduler.SetValidator(() => !Me.HasUnitState(UnitState.Casting));
    }

    public override void Reset()
    {
        Scheduler.CancelAll();
        _combatScheduler.CancelAll();

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               // Find a spell that targets friendly and applies an aura (these are generally buffs)
                               var spellInfo = SelectSpell(Me, 0, 0, SelectTargetType.AnyFriend, 0, 0, SelectEffect.Aura);

                               if (spellInfo != null)
                                   DoCast(Me, spellInfo.Id);

                               task.Repeat(TimeSpan.FromMinutes(10));
                           });
    }

    public override void ReceiveEmote(Player player, TextEmotes textEmote)
    {
        switch (Me.Entry)
        {
            case CreatureIds.StormwindCityGuard:
            case CreatureIds.StormwindCityPatroller:
            case CreatureIds.OrgrimmarGrunt:
                break;
            default:
                return;
        }

        if (!Me.IsFriendlyTo(player))
            return;

        DoReplyToTextEmote(textEmote);
    }

    public override void JustEngagedWith(Unit who)
    {
        if (Me.Entry == CreatureIds.CenarionHoldInfantry)
            Talk(TextIds.SayGuardSilAggro, who);

        _combatScheduler.Schedule(TimeSpan.FromSeconds(1),
                                  task =>
                                  {
                                      var victim = Me.Victim;

                                      if (!Me.IsAttackReady() ||
                                          !Me.IsWithinMeleeRange(victim))
                                      {
                                          task.Repeat();

                                          return;
                                      }

                                      if (RandomHelper.randChance(20))
                                      {
                                          var spellInfo = SelectSpell(Me.Victim, 0, 0, SelectTargetType.AnyEnemy, 0, SharedConst.NominalMeleeRange, SelectEffect.DontCare);

                                          if (spellInfo != null)
                                          {
                                              Me.ResetAttackTimer();
                                              DoCastVictim(spellInfo.Id);
                                              task.Repeat();

                                              return;
                                          }
                                      }

                                      Me.AttackerStateUpdate(victim);
                                      Me.ResetAttackTimer();
                                      task.Repeat();
                                  });

        _combatScheduler.Schedule(TimeSpan.FromSeconds(5),
                                  task =>
                                  {
                                      var healing = false;
                                      SpellInfo spellInfo = null;

                                      // Select a healing spell if less than 30% hp and Only 33% of the Time
                                      if (Me.HealthBelowPct(30) &&
                                          RandomHelper.randChance(33))
                                          spellInfo = SelectSpell(Me, 0, 0, SelectTargetType.AnyFriend, 0, 0, SelectEffect.Healing);

                                      // No healing spell available, check if we can cast a ranged spell
                                      if (spellInfo != null)
                                          healing = true;
                                      else
                                          spellInfo = SelectSpell(Me.Victim, 0, 0, SelectTargetType.AnyEnemy, SharedConst.NominalMeleeRange, 0, SelectEffect.DontCare);

                                      // Found a spell
                                      if (spellInfo != null)
                                      {
                                          if (healing)
                                              DoCast(Me, spellInfo.Id);
                                          else
                                              DoCastVictim(spellInfo.Id);

                                          task.Repeat(TimeSpan.FromSeconds(5));
                                      }
                                      else
                                      {
                                          task.Repeat(TimeSpan.FromSeconds(1));
                                      }
                                  });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);

        if (!UpdateVictim())
            return;

        _combatScheduler.Update(diff);
    }

    private void DoReplyToTextEmote(TextEmotes emote)
    {
        switch (emote)
        {
            case TextEmotes.Kiss:
                Me.HandleEmoteCommand(Emote.OneshotBow);

                break;
            case TextEmotes.Wave:
                Me.HandleEmoteCommand(Emote.OneshotWave);

                break;
            case TextEmotes.Salute:
                Me.HandleEmoteCommand(Emote.OneshotSalute);

                break;
            case TextEmotes.Shy:
                Me.HandleEmoteCommand(Emote.OneshotFlex);

                break;
            case TextEmotes.Rude:
            case TextEmotes.Chicken:
                Me.HandleEmoteCommand(Emote.OneshotPoint);

                break;
            
        }
    }
}

[Script]
internal class npc_guard_shattrath_faction : GuardAI
{
    public npc_guard_shattrath_faction(Creature creature) : base(creature)
    {
        Scheduler.SetValidator(() => !Me.HasUnitState(UnitState.Casting));
    }

    public override void Reset()
    {
        Scheduler.CancelAll();
    }

    public override void JustEngagedWith(Unit who)
    {
        ScheduleVanish();
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, DoMeleeAttackIfReady);
    }

    private void ScheduleVanish()
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           task =>
                           {
                               var temp = Me.Victim;

                               if (temp && temp.IsTypeId(TypeId.Player))
                               {
                                   DoCast(temp, Me.Entry == CreatureIds.AldorVindicator ? SpellIds.BanishedShattrathS : SpellIds.BanishedShattrathA);
                                   var playerGUID = temp.GUID;

                                   task.Schedule(TimeSpan.FromSeconds(9),
                                                 task =>
                                                 {
                                                     var temp = Global.ObjAccessor.GetUnit(Me, playerGUID);

                                                     if (temp)
                                                     {
                                                         temp.CastSpell(temp, SpellIds.Exile, true);
                                                         temp.CastSpell(temp, SpellIds.BanishTeleport, true);
                                                     }

                                                     ScheduleVanish();
                                                 });
                               }
                               else
                               {
                                   task.Repeat();
                               }
                           });
    }
}