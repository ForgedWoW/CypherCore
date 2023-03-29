// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Karazhan.TerestianIllhoof;

internal struct SpellIds
{
    public const uint ShadowBolt = 30055;
    public const uint SummonImp = 30066;
    public const uint FiendishPortal1 = 30171;
    public const uint FiendishPortal2 = 30179;
    public const uint Berserk = 32965;
    public const uint SummonFiendishImp = 30184;
    public const uint BrokenPact = 30065;
    public const uint AmplifyFlames = 30053;
    public const uint Firebolt = 30050;
    public const uint SummonDemonchains = 30120;
    public const uint DemonChains = 30206;
    public const uint Sacrifice = 30115;
}

internal struct TextIds
{
    public const uint SaySlay = 0;
    public const uint SayDeath = 1;
    public const uint SayAggro = 2;
    public const uint SaySacrifice = 3;
    public const uint SaySummonPortal = 4;
}

internal struct MiscConst
{
    public const uint NpcFiendishPortal = 17265;
    public const int ActionDespawnImps = 1;
}

[Script]
internal class boss_terestian : BossAI
{
    public boss_terestian(Creature creature) : base(creature, DataTypes.Terestian) { }

    public override void Reset()
    {
        EntryCheckPredicate pred = new(MiscConst.NpcFiendishPortal);
        Summons.DoAction(MiscConst.ActionDespawnImps, pred);
        _Reset();

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.MaxThreat, 0);

                               if (target)
                                   DoCast(target, SpellIds.ShadowBolt);

                               task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                      {
                                                                          Me.RemoveAura(SpellIds.BrokenPact);
                                                                          DoCastAOE(SpellIds.SummonImp, new CastSpellExtraArgs(true));
                                                                      }));

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);

                               if (target)
                               {
                                   DoCast(target, SpellIds.Sacrifice, new CastSpellExtraArgs(true));
                                   target.CastSpell(target, SpellIds.SummonDemonchains, true);
                                   Talk(TextIds.SaySacrifice);
                               }

                               task.Repeat(TimeSpan.FromSeconds(42));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               Talk(TextIds.SaySummonPortal);
                               DoCastAOE(SpellIds.FiendishPortal1);
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(11), task => { DoCastAOE(SpellIds.FiendishPortal2, new CastSpellExtraArgs(true)); });
        Scheduler.Schedule(TimeSpan.FromMinutes(10), task => { DoCastSelf(SpellIds.Berserk, new CastSpellExtraArgs(true)); });
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.SayAggro);
    }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        if (spellInfo.Id == SpellIds.BrokenPact)
            Scheduler.Schedule(TimeSpan.FromSeconds(32),
                               (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                          {
                                                                              Me.RemoveAura(SpellIds.BrokenPact);
                                                                              DoCastAOE(SpellIds.SummonImp, new CastSpellExtraArgs(true));
                                                                          }));
    }

    public override void KilledUnit(Unit victim)
    {
        if (victim.IsPlayer)
            Talk(TextIds.SaySlay);
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SayDeath);
        EntryCheckPredicate pred = new(MiscConst.NpcFiendishPortal);
        Summons.DoAction(MiscConst.ActionDespawnImps, pred);
        _JustDied();
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}

[Script]
internal class npc_kilrek : ScriptedAI
{
    public npc_kilrek(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.AmplifyFlames);
                               task.Repeat(TimeSpan.FromSeconds(9));
                           });
    }

    public override void JustDied(Unit killer)
    {
        DoCastAOE(SpellIds.BrokenPact, new CastSpellExtraArgs(true));
        Me.DespawnOrUnsummon(TimeSpan.FromSeconds(15));
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => { DoMeleeAttackIfReady(); });
    }
}

[Script]
internal class npc_demon_chain : PassiveAI
{
    private ObjectGuid _sacrificeGUID;

    public npc_demon_chain(Creature creature) : base(creature) { }

    public override void IsSummonedBy(WorldObject summoner)
    {
        _sacrificeGUID = summoner.GUID;
        DoCastSelf(SpellIds.DemonChains, new CastSpellExtraArgs(true));
    }

    public override void JustDied(Unit killer)
    {
        var sacrifice = Global.ObjAccessor.GetUnit(Me, _sacrificeGUID);

        if (sacrifice)
            sacrifice.RemoveAura(SpellIds.Sacrifice);
    }
}

[Script]
internal class npc_fiendish_portal : PassiveAI
{
    private readonly SummonList _summons;

    public npc_fiendish_portal(Creature creature) : base(creature)
    {
        _summons = new SummonList(Me);
    }

    public override void Reset()
    {
        Scheduler.Schedule(TimeSpan.FromMilliseconds(2400),
                           TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastAOE(SpellIds.SummonFiendishImp, new CastSpellExtraArgs(true));
                               task.Repeat();
                           });
    }

    public override void DoAction(int action)
    {
        if (action == MiscConst.ActionDespawnImps)
            _summons.DespawnAll();
    }

    public override void JustSummoned(Creature summon)
    {
        _summons.Summon(summon);
        DoZoneInCombat(summon);
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}

[Script]
internal class npc_fiendish_imp : ScriptedAI
{
    public npc_fiendish_imp(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCastVictim(SpellIds.Firebolt);
                               task.Repeat(TimeSpan.FromMilliseconds(2400));
                           });

        Me.ApplySpellImmune(0, SpellImmunity.School, SpellSchoolMask.Fire, true);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}