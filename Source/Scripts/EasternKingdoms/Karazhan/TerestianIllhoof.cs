// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Karazhan.TerestianIllhoof;

internal struct SpellIds
{
    public const uint SHADOW_BOLT = 30055;
    public const uint SUMMON_IMP = 30066;
    public const uint FIENDISH_PORTAL1 = 30171;
    public const uint FIENDISH_PORTAL2 = 30179;
    public const uint BERSERK = 32965;
    public const uint SUMMON_FIENDISH_IMP = 30184;
    public const uint BROKEN_PACT = 30065;
    public const uint AMPLIFY_FLAMES = 30053;
    public const uint FIREBOLT = 30050;
    public const uint SUMMON_DEMONCHAINS = 30120;
    public const uint DEMON_CHAINS = 30206;
    public const uint SACRIFICE = 30115;
}

internal struct TextIds
{
    public const uint SAY_SLAY = 0;
    public const uint SAY_DEATH = 1;
    public const uint SAY_AGGRO = 2;
    public const uint SAY_SACRIFICE = 3;
    public const uint SAY_SUMMON_PORTAL = 4;
}

internal struct MiscConst
{
    public const uint NPC_FIENDISH_PORTAL = 17265;
    public const int ACTION_DESPAWN_IMPS = 1;
}

[Script]
internal class BossTerestian : BossAI
{
    public BossTerestian(Creature creature) : base(creature, DataTypes.TERESTIAN) { }

    public override void Reset()
    {
        EntryCheckPredicate pred = new(MiscConst.NPC_FIENDISH_PORTAL);
        Summons.DoAction(MiscConst.ACTION_DESPAWN_IMPS, pred);
        _Reset();

        Scheduler.Schedule(TimeSpan.FromSeconds(1),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.MaxThreat, 0);

                               if (target)
                                   DoCast(target, SpellIds.SHADOW_BOLT);

                               task.Repeat(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                      {
                                                                          Me.RemoveAura(SpellIds.BROKEN_PACT);
                                                                          DoCastAOE(SpellIds.SUMMON_IMP, new CastSpellExtraArgs(true));
                                                                      }));

        Scheduler.Schedule(TimeSpan.FromSeconds(30),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);

                               if (target)
                               {
                                   DoCast(target, SpellIds.SACRIFICE, new CastSpellExtraArgs(true));
                                   target.SpellFactory.CastSpell(target, SpellIds.SUMMON_DEMONCHAINS, true);
                                   Talk(TextIds.SAY_SACRIFICE);
                               }

                               task.Repeat(TimeSpan.FromSeconds(42));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               Talk(TextIds.SAY_SUMMON_PORTAL);
                               DoCastAOE(SpellIds.FIENDISH_PORTAL1);
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(11), task => { DoCastAOE(SpellIds.FIENDISH_PORTAL2, new CastSpellExtraArgs(true)); });
        Scheduler.Schedule(TimeSpan.FromMinutes(10), task => { DoCastSelf(SpellIds.BERSERK, new CastSpellExtraArgs(true)); });
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.SAY_AGGRO);
    }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        if (spellInfo.Id == SpellIds.BROKEN_PACT)
            Scheduler.Schedule(TimeSpan.FromSeconds(32),
                               (Action<Framework.Dynamic.TaskContext>)(task =>
                                                                          {
                                                                              Me.RemoveAura(SpellIds.BROKEN_PACT);
                                                                              DoCastAOE(SpellIds.SUMMON_IMP, new CastSpellExtraArgs(true));
                                                                          }));
    }

    public override void KilledUnit(Unit victim)
    {
        if (victim.IsPlayer)
            Talk(TextIds.SAY_SLAY);
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_DEATH);
        EntryCheckPredicate pred = new(MiscConst.NPC_FIENDISH_PORTAL);
        Summons.DoAction(MiscConst.ACTION_DESPAWN_IMPS, pred);
        _JustDied();
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }
}

[Script]
internal class NPCKilrek : ScriptedAI
{
    public NPCKilrek(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastVictim(SpellIds.AMPLIFY_FLAMES);
                               task.Repeat(TimeSpan.FromSeconds(9));
                           });
    }

    public override void JustDied(Unit killer)
    {
        DoCastAOE(SpellIds.BROKEN_PACT, new CastSpellExtraArgs(true));
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
internal class NPCDemonChain : PassiveAI
{
    private ObjectGuid _sacrificeGUID;

    public NPCDemonChain(Creature creature) : base(creature) { }

    public override void IsSummonedBy(WorldObject summoner)
    {
        _sacrificeGUID = summoner.GUID;
        DoCastSelf(SpellIds.DEMON_CHAINS, new CastSpellExtraArgs(true));
    }

    public override void JustDied(Unit killer)
    {
        var sacrifice = Global.ObjAccessor.GetUnit(Me, _sacrificeGUID);

        if (sacrifice)
            sacrifice.RemoveAura(SpellIds.SACRIFICE);
    }
}

[Script]
internal class NPCFiendishPortal : PassiveAI
{
    private readonly SummonList _summons;

    public NPCFiendishPortal(Creature creature) : base(creature)
    {
        _summons = new SummonList(Me);
    }

    public override void Reset()
    {
        Scheduler.Schedule(TimeSpan.FromMilliseconds(2400),
                           TimeSpan.FromSeconds(8),
                           task =>
                           {
                               DoCastAOE(SpellIds.SUMMON_FIENDISH_IMP, new CastSpellExtraArgs(true));
                               task.Repeat();
                           });
    }

    public override void DoAction(int action)
    {
        if (action == MiscConst.ACTION_DESPAWN_IMPS)
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
internal class NPCFiendishImp : ScriptedAI
{
    public NPCFiendishImp(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(2),
                           task =>
                           {
                               DoCastVictim(SpellIds.FIREBOLT);
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