// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Forged.MapServer.Arenas.Zones;

internal class DalaranSewersArena : Arena
{
    public DalaranSewersArena(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
    {
        _events = new EventMap();
    }

    public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        if (GetStatus() != BattlegroundStatus.InProgress)
            return;

        switch (trigger)
        {
            case 5347:
            case 5348:
                // Remove effects of Demonic Circle Summon
                player.RemoveAura(DalaranSewersSpells.DemonicCircle);

                // Someone has get back into the pipes and the knockback has already been performed,
                // so we reset the knockback count for kicking the player again into the arena.
                _events.ScheduleEvent(DalaranSewersEvents.PipeKnockback, DalaranSewersData.PipeKnockbackDelay);

                break;

            default:
                base.HandleAreaTrigger(player, trigger, entered);

                break;
        }
    }

    public override void PostUpdateImpl(uint diff)
    {
        if (GetStatus() != BattlegroundStatus.InProgress)
            return;

        _events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case DalaranSewersEvents.WaterfallWarning:
                    // Add the water
                    DoorClose(DalaranSewersObjectTypes.Water2);
                    _events.ScheduleEvent(DalaranSewersEvents.WaterfallOn, DalaranSewersData.WaterWarningDuration);

                    break;

                case DalaranSewersEvents.WaterfallOn:
                    // Active collision and start knockback timer
                    DoorClose(DalaranSewersObjectTypes.Water1);
                    _events.ScheduleEvent(DalaranSewersEvents.WaterfallOff, DalaranSewersData.WaterfallDuration);
                    _events.ScheduleEvent(DalaranSewersEvents.WaterfallKnockback, DalaranSewersData.WaterfallKnockbackTimer);

                    break;

                case DalaranSewersEvents.WaterfallOff:
                    // Remove collision and water
                    DoorOpen(DalaranSewersObjectTypes.Water1);
                    DoorOpen(DalaranSewersObjectTypes.Water2);
                    _events.CancelEvent(DalaranSewersEvents.WaterfallKnockback);
                    _events.ScheduleEvent(DalaranSewersEvents.WaterfallWarning, DalaranSewersData.WaterfallTimerMin, DalaranSewersData.WaterfallTimerMax);

                    break;

                case DalaranSewersEvents.WaterfallKnockback:
                {
                    // Repeat knockback while the waterfall still active
                    var waterSpout = GetBGCreature(DalaranSewersCreatureTypes.WaterfallKnockback);

                    if (waterSpout)
                        waterSpout.CastSpell(waterSpout, DalaranSewersSpells.WaterSpout, true);

                    _events.ScheduleEvent(eventId, DalaranSewersData.WaterfallKnockbackTimer);
                }

                break;

                case DalaranSewersEvents.PipeKnockback:
                {
                    for (var i = DalaranSewersCreatureTypes.PipeKnockback1; i <= DalaranSewersCreatureTypes.PipeKnockback2; ++i)
                    {
                        var waterSpout = GetBGCreature(i);

                        if (waterSpout)
                            waterSpout.CastSpell(waterSpout, DalaranSewersSpells.Flush, true);
                    }
                }

                break;
            }
        });
    }

    public override bool SetupBattleground()
    {
        var result = true;
        result &= AddObject(DalaranSewersObjectTypes.Door1, DalaranSewersGameObjects.Door1, 1350.95f, 817.2f, 20.8096f, 3.15f, 0, 0, 0.99627f, 0.0862864f);
        result &= AddObject(DalaranSewersObjectTypes.Door2, DalaranSewersGameObjects.Door2, 1232.65f, 764.913f, 20.0729f, 6.3f, 0, 0, 0.0310211f, -0.999519f);

        if (!result)
        {
            Log.Logger.Error("DalaranSewersArena: Failed to spawn door object!");

            return false;
        }

        // buffs
        result &= AddObject(DalaranSewersObjectTypes.Buff1, DalaranSewersGameObjects.Buff1, 1291.7f, 813.424f, 7.11472f, 4.64562f, 0, 0, 0.730314f, -0.683111f, 120);
        result &= AddObject(DalaranSewersObjectTypes.Buff2, DalaranSewersGameObjects.Buff2, 1291.7f, 768.911f, 7.11472f, 1.55194f, 0, 0, 0.700409f, 0.713742f, 120);

        if (!result)
        {
            Log.Logger.Error("DalaranSewersArena: Failed to spawn buff object!");

            return false;
        }

        result &= AddObject(DalaranSewersObjectTypes.Water1, DalaranSewersGameObjects.Water1, 1291.56f, 790.837f, 7.1f, 3.14238f, 0, 0, 0.694215f, -0.719768f, 120);
        result &= AddObject(DalaranSewersObjectTypes.Water2, DalaranSewersGameObjects.Water2, 1291.56f, 790.837f, 7.1f, 3.14238f, 0, 0, 0.694215f, -0.719768f, 120);
        result &= AddCreature(DalaranSewersData.NpcWaterSpout, DalaranSewersCreatureTypes.WaterfallKnockback, 1292.587f, 790.2205f, 7.19796f, 3.054326f);
        result &= AddCreature(DalaranSewersData.NpcWaterSpout, DalaranSewersCreatureTypes.PipeKnockback1, 1369.977f, 817.2882f, 16.08718f, 3.106686f);
        result &= AddCreature(DalaranSewersData.NpcWaterSpout, DalaranSewersCreatureTypes.PipeKnockback2, 1212.833f, 765.3871f, 16.09484f, 0.0f);

        if (!result)
        {
            Log.Logger.Error("DalaranSewersArena: Failed to spawn collision object!");

            return false;
        }

        return true;
    }

    public override void StartingEventCloseDoors()
    {
        for (var i = DalaranSewersObjectTypes.Door1; i <= DalaranSewersObjectTypes.Door2; ++i)
            SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
    }

    public override void StartingEventOpenDoors()
    {
        for (var i = DalaranSewersObjectTypes.Door1; i <= DalaranSewersObjectTypes.Door2; ++i)
            DoorOpen(i);

        for (var i = DalaranSewersObjectTypes.Buff1; i <= DalaranSewersObjectTypes.Buff2; ++i)
            SpawnBGObject(i, 60);

        _events.ScheduleEvent(DalaranSewersEvents.WaterfallWarning, DalaranSewersData.WaterfallTimerMin, DalaranSewersData.WaterfallTimerMax);
        _events.ScheduleEvent(DalaranSewersEvents.PipeKnockback, DalaranSewersData.PipeKnockbackFirstDelay);

        SpawnBGObject(DalaranSewersObjectTypes.Water2, BattlegroundConst.RespawnImmediately);

        DoorOpen(DalaranSewersObjectTypes.Water1); // Turn off collision
        DoorOpen(DalaranSewersObjectTypes.Water2);

        // Remove effects of Demonic Circle Summon
        foreach (var pair in GetPlayers())
        {
            var player = _GetPlayer(pair, "BattlegroundDS::StartingEventOpenDoors");

            if (player)
                player.RemoveAura(DalaranSewersSpells.DemonicCircle);
        }
    }
}