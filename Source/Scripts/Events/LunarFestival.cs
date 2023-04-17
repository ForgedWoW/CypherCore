// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.m_Events.LunarFestival;

internal struct SpellIds
{
    //Fireworks
    public const uint ROCKET_BLUE = 26344;
    public const uint ROCKET_GREEN = 26345;
    public const uint ROCKET_PURPLE = 26346;
    public const uint ROCKET_RED = 26347;
    public const uint ROCKET_WHITE = 26348;
    public const uint ROCKET_YELLOW = 26349;
    public const uint ROCKET_BIG_BLUE = 26351;
    public const uint ROCKET_BIG_GREEN = 26352;
    public const uint ROCKET_BIG_PURPLE = 26353;
    public const uint ROCKET_BIG_RED = 26354;
    public const uint ROCKET_BIG_WHITE = 26355;
    public const uint ROCKET_BIG_YELLOW = 26356;
    public const uint LUNAR_FORTUNE = 26522;

    //Omen
    public const uint OMEN_CLEAVE = 15284;
    public const uint OMEN_STARFALL = 26540;
    public const uint OMEN_SUMMON_SPOTLIGHT = 26392;
    public const uint ELUNE_CANDLE = 26374;

    //EluneCandle
    public const uint ELUNE_CANDLE_OMEN_HEAD = 26622;
    public const uint ELUNE_CANDLE_OMEN_CHEST = 26624;
    public const uint ELUNE_CANDLE_OMEN_HAND_R = 26625;
    public const uint ELUNE_CANDLE_OMEN_HAND_L = 26649;
    public const uint ELUNE_CANDLE_NORMAL = 26636;
}

internal struct CreatureIds
{
    //Fireworks
    public const uint OMEN = 15467;
    public const uint MINION_OF_OMEN = 15466;
    public const uint FIREWORK_BLUE = 15879;
    public const uint FIREWORK_GREEN = 15880;
    public const uint FIREWORK_PURPLE = 15881;
    public const uint FIREWORK_RED = 15882;
    public const uint FIREWORK_YELLOW = 15883;
    public const uint FIREWORK_WHITE = 15884;
    public const uint FIREWORK_BIG_BLUE = 15885;
    public const uint FIREWORK_BIG_GREEN = 15886;
    public const uint FIREWORK_BIG_PURPLE = 15887;
    public const uint FIREWORK_BIG_RED = 15888;
    public const uint FIREWORK_BIG_YELLOW = 15889;
    public const uint FIREWORK_BIG_WHITE = 15890;

    public const uint CLUSTER_BLUE = 15872;
    public const uint CLUSTER_RED = 15873;
    public const uint CLUSTER_GREEN = 15874;
    public const uint CLUSTER_PURPLE = 15875;
    public const uint CLUSTER_WHITE = 15876;
    public const uint CLUSTER_YELLOW = 15877;
    public const uint CLUSTER_BIG_BLUE = 15911;
    public const uint CLUSTER_BIG_GREEN = 15912;
    public const uint CLUSTER_BIG_PURPLE = 15913;
    public const uint CLUSTER_BIG_RED = 15914;
    public const uint CLUSTER_BIG_WHITE = 15915;
    public const uint CLUSTER_BIG_YELLOW = 15916;
    public const uint CLUSTER_ELUNE = 15918;
}

internal struct GameObjectIds
{
    //Fireworks
    public const uint FIREWORK_LAUNCHER1 = 180771;
    public const uint FIREWORK_LAUNCHER2 = 180868;
    public const uint FIREWORK_LAUNCHER3 = 180850;
    public const uint CLUSTER_LAUNCHER1 = 180772;
    public const uint CLUSTER_LAUNCHER2 = 180859;
    public const uint CLUSTER_LAUNCHER3 = 180869;
    public const uint CLUSTER_LAUNCHER4 = 180874;

    //Omen
    public const uint ELUNE_TRAP1 = 180876;
    public const uint ELUNE_TRAP2 = 180877;
}

internal struct MiscConst
{
    //Fireworks
    public const uint ANIM_GO_LAUNCH_FIREWORK = 3;
    public const uint ZONE_MOONGLADE = 493;

    //Omen
    public static Position OmenSummonPos = new(7558.993f, -2839.999f, 450.0214f, 4.46f);
}

[Script]
internal class NPCFirework : ScriptedAI
{
    public NPCFirework(Creature creature) : base(creature) { }

    public override void Reset()
    {
        var launcher = FindNearestLauncher();

        if (launcher)
        {
            launcher.SendCustomAnim(MiscConst.ANIM_GO_LAUNCH_FIREWORK);
            Me.Location.Orientation = launcher.Location.Orientation + MathF.PI / 2;
        }
        else
        {
            return;
        }

        if (IsCluster())
        {
            // Check if we are near Elune'ara lake south, if so try to summon Omen or a minion
            if (Me.Zone == MiscConst.ZONE_MOONGLADE)
                if (!Me.FindNearestCreature(CreatureIds.OMEN, 100.0f) &&
                    Me.GetDistance2d(MiscConst.OmenSummonPos.X, MiscConst.OmenSummonPos.Y) <= 100.0f)
                    switch (RandomHelper.URand(0, 9))
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                            Creature minion = Me.SummonCreature(CreatureIds.MINION_OF_OMEN, Me.Location.X + RandomHelper.FRand(-5.0f, 5.0f), Me.Location.Y + RandomHelper.FRand(-5.0f, 5.0f), Me.Location.Z, 0.0f, TempSummonType.CorpseTimedDespawn, TimeSpan.FromSeconds(20));

                            if (minion)
                                minion.AI.AttackStart(Me.SelectNearestPlayer(20.0f));

                            break;
                        case 9:
                            Me.SummonCreature(CreatureIds.OMEN, MiscConst.OmenSummonPos);

                            break;
                    }

            if (Me.Entry == CreatureIds.CLUSTER_ELUNE)
                DoCast(SpellIds.LUNAR_FORTUNE);

            var displacement = 0.7f;

            for (byte i = 0; i < 4; i++)
                Me.SummonGameObject(GetFireworkGameObjectId(), Me.Location.X + (i % 2 == 0 ? displacement : -displacement), Me.Location.Y + (i > 1 ? displacement : -displacement), Me.Location.Z + 4.0f, Me.Location.Orientation, Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(Me.Location.Orientation, 0.0f, 0.0f)), TimeSpan.FromSeconds(1));
        }
        else
            //me.SpellFactory.CastSpell(me, GetFireworkSpell(me.GetEntry()), true);
        {
            Me.SpellFactory.CastSpell(Me.Location, GetFireworkSpell(Me.Entry), new CastSpellExtraArgs(true));
        }
    }

    private bool IsCluster()
    {
        switch (Me.Entry)
        {
            case CreatureIds.FIREWORK_BLUE:
            case CreatureIds.FIREWORK_GREEN:
            case CreatureIds.FIREWORK_PURPLE:
            case CreatureIds.FIREWORK_RED:
            case CreatureIds.FIREWORK_YELLOW:
            case CreatureIds.FIREWORK_WHITE:
            case CreatureIds.FIREWORK_BIG_BLUE:
            case CreatureIds.FIREWORK_BIG_GREEN:
            case CreatureIds.FIREWORK_BIG_PURPLE:
            case CreatureIds.FIREWORK_BIG_RED:
            case CreatureIds.FIREWORK_BIG_YELLOW:
            case CreatureIds.FIREWORK_BIG_WHITE:
                return false;
            case CreatureIds.CLUSTER_BLUE:
            case CreatureIds.CLUSTER_GREEN:
            case CreatureIds.CLUSTER_PURPLE:
            case CreatureIds.CLUSTER_RED:
            case CreatureIds.CLUSTER_YELLOW:
            case CreatureIds.CLUSTER_WHITE:
            case CreatureIds.CLUSTER_BIG_BLUE:
            case CreatureIds.CLUSTER_BIG_GREEN:
            case CreatureIds.CLUSTER_BIG_PURPLE:
            case CreatureIds.CLUSTER_BIG_RED:
            case CreatureIds.CLUSTER_BIG_YELLOW:
            case CreatureIds.CLUSTER_BIG_WHITE:
            case CreatureIds.CLUSTER_ELUNE:
            default:
                return true;
        }
    }

    private GameObject FindNearestLauncher()
    {
        GameObject launcher = null;

        if (IsCluster())
        {
            var launcher1 = GetClosestGameObjectWithEntry(Me, GameObjectIds.CLUSTER_LAUNCHER1, 0.5f);
            var launcher2 = GetClosestGameObjectWithEntry(Me, GameObjectIds.CLUSTER_LAUNCHER2, 0.5f);
            var launcher3 = GetClosestGameObjectWithEntry(Me, GameObjectIds.CLUSTER_LAUNCHER3, 0.5f);
            var launcher4 = GetClosestGameObjectWithEntry(Me, GameObjectIds.CLUSTER_LAUNCHER4, 0.5f);

            if (launcher1)
                launcher = launcher1;
            else if (launcher2)
                launcher = launcher2;
            else if (launcher3)
                launcher = launcher3;
            else if (launcher4)
                launcher = launcher4;
        }
        else
        {
            var launcher1 = GetClosestGameObjectWithEntry(Me, GameObjectIds.FIREWORK_LAUNCHER1, 0.5f);
            var launcher2 = GetClosestGameObjectWithEntry(Me, GameObjectIds.FIREWORK_LAUNCHER2, 0.5f);
            var launcher3 = GetClosestGameObjectWithEntry(Me, GameObjectIds.FIREWORK_LAUNCHER3, 0.5f);

            if (launcher1)
                launcher = launcher1;
            else if (launcher2)
                launcher = launcher2;
            else if (launcher3)
                launcher = launcher3;
        }

        return launcher;
    }

    private uint GetFireworkSpell(uint entry)
    {
        switch (entry)
        {
            case CreatureIds.FIREWORK_BLUE:
                return SpellIds.ROCKET_BLUE;
            case CreatureIds.FIREWORK_GREEN:
                return SpellIds.ROCKET_GREEN;
            case CreatureIds.FIREWORK_PURPLE:
                return SpellIds.ROCKET_PURPLE;
            case CreatureIds.FIREWORK_RED:
                return SpellIds.ROCKET_RED;
            case CreatureIds.FIREWORK_YELLOW:
                return SpellIds.ROCKET_YELLOW;
            case CreatureIds.FIREWORK_WHITE:
                return SpellIds.ROCKET_WHITE;
            case CreatureIds.FIREWORK_BIG_BLUE:
                return SpellIds.ROCKET_BIG_BLUE;
            case CreatureIds.FIREWORK_BIG_GREEN:
                return SpellIds.ROCKET_BIG_GREEN;
            case CreatureIds.FIREWORK_BIG_PURPLE:
                return SpellIds.ROCKET_BIG_PURPLE;
            case CreatureIds.FIREWORK_BIG_RED:
                return SpellIds.ROCKET_BIG_RED;
            case CreatureIds.FIREWORK_BIG_YELLOW:
                return SpellIds.ROCKET_BIG_YELLOW;
            case CreatureIds.FIREWORK_BIG_WHITE:
                return SpellIds.ROCKET_BIG_WHITE;
            default:
                return 0;
        }
    }

    private uint GetFireworkGameObjectId()
    {
        uint spellId = 0;

        switch (Me.Entry)
        {
            case CreatureIds.CLUSTER_BLUE:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_BLUE);

                break;
            case CreatureIds.CLUSTER_GREEN:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_GREEN);

                break;
            case CreatureIds.CLUSTER_PURPLE:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_PURPLE);

                break;
            case CreatureIds.CLUSTER_RED:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_RED);

                break;
            case CreatureIds.CLUSTER_YELLOW:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_YELLOW);

                break;
            case CreatureIds.CLUSTER_WHITE:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_WHITE);

                break;
            case CreatureIds.CLUSTER_BIG_BLUE:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_BIG_BLUE);

                break;
            case CreatureIds.CLUSTER_BIG_GREEN:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_BIG_GREEN);

                break;
            case CreatureIds.CLUSTER_BIG_PURPLE:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_BIG_PURPLE);

                break;
            case CreatureIds.CLUSTER_BIG_RED:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_BIG_RED);

                break;
            case CreatureIds.CLUSTER_BIG_YELLOW:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_BIG_YELLOW);

                break;
            case CreatureIds.CLUSTER_BIG_WHITE:
                spellId = GetFireworkSpell(CreatureIds.FIREWORK_BIG_WHITE);

                break;
            case CreatureIds.CLUSTER_ELUNE:
                spellId = GetFireworkSpell(RandomHelper.URand(CreatureIds.FIREWORK_BLUE, CreatureIds.FIREWORK_WHITE));

                break;
        }

        var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

        if (spellInfo != null &&
            spellInfo.GetEffect(0).Effect == SpellEffectName.SummonObjectWild)
            return (uint)spellInfo.GetEffect(0).MiscValue;

        return 0;
    }
}

[Script]
internal class NPCOmen : ScriptedAI
{
    public NPCOmen(Creature creature) : base(creature)
    {
        Me.SetImmuneToPC(true);
        Me.MotionMaster.MovePoint(1, 7549.977f, -2855.137f, 456.9678f);
    }

    public override void MovementInform(MovementGeneratorType type, uint pointId)
    {
        if (type != MovementGeneratorType.Point)
            return;

        if (pointId == 1)
        {
            Me.SetHomePosition(Me.Location.X, Me.Location.Y, Me.Location.Z, Me.Location.Orientation);
            Me.SetImmuneToPC(false);
            var player = Me.SelectNearestPlayer(40.0f);

            if (player)
                AttackStart(player);
        }
    }

    public override void JustEngagedWith(Unit attacker)
    {
        Scheduler.CancelAll();

        Scheduler.Schedule(TimeSpan.FromSeconds(3),
                           TimeSpan.FromSeconds(5),
                           task =>
                           {
                               DoCastVictim(SpellIds.OMEN_CLEAVE);
                               task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(8),
                           TimeSpan.FromSeconds(10),
                           1,
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0);

                               if (target)
                                   DoCast(target, SpellIds.OMEN_STARFALL);

                               task.Repeat(TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(16));
                           });
    }

    public override void JustDied(Unit killer)
    {
        DoCast(SpellIds.OMEN_SUMMON_SPOTLIGHT);
    }

    public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
    {
        if (spellInfo.Id == SpellIds.ELUNE_CANDLE)
        {
            if (Me.HasAura(SpellIds.OMEN_STARFALL))
                Me.RemoveAura(SpellIds.OMEN_STARFALL);

            Scheduler.RescheduleGroup(1, TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(16));
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff);

        DoMeleeAttackIfReady();
    }
}

[Script]
internal class NPCGiantSpotlight : ScriptedAI
{
    public NPCGiantSpotlight(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Scheduler.CancelAll();

        Scheduler.Schedule(TimeSpan.FromMinutes(5),
                           task =>
                           {
                               var trap = Me.FindNearestGameObject(GameObjectIds.ELUNE_TRAP1, 5.0f);

                               if (trap)
                                   trap.RemoveFromWorld();

                               trap = Me.FindNearestGameObject(GameObjectIds.ELUNE_TRAP2, 5.0f);

                               if (trap)
                                   trap.RemoveFromWorld();

                               var omen = Me.FindNearestCreature(CreatureIds.OMEN, 5.0f, false);

                               if (omen)
                                   omen.DespawnOrUnsummon();

                               Me.DespawnOrUnsummon();
                           });
    }

    public override void UpdateAI(uint diff)
    {
        Scheduler.Update(diff);
    }
}

[Script] // 26374 - Elune's Candle
internal class SpellLunarFestivalEluneCandle : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        uint spellId = 0;

        if (HitUnit.Entry == CreatureIds.OMEN)
            switch (RandomHelper.URand(0, 3))
            {
                case 0:
                    spellId = SpellIds.ELUNE_CANDLE_OMEN_HEAD;

                    break;
                case 1:
                    spellId = SpellIds.ELUNE_CANDLE_OMEN_CHEST;

                    break;
                case 2:
                    spellId = SpellIds.ELUNE_CANDLE_OMEN_HAND_R;

                    break;
                case 3:
                    spellId = SpellIds.ELUNE_CANDLE_OMEN_HAND_L;

                    break;
            }
        else
            spellId = SpellIds.ELUNE_CANDLE_NORMAL;

        Caster.SpellFactory.CastSpell(HitUnit, spellId, true);
    }
}