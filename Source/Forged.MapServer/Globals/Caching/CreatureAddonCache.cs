// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.E;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Spells;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class CreatureAddonCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly IConfiguration _configuration;
    private readonly CreatureDataCache _creatureDataCache;
    private readonly SpellManager _spellManager;
    private readonly DB6Storage<CreatureDisplayInfoRecord> _creatureDisplayInfoRecords;
    private readonly DB6Storage<EmotesRecord> _emotesRecords;
    private readonly DB6Storage<AnimKitRecord> _animKitRecords;
    private readonly Dictionary<ulong, CreatureAddon> _creatureAddonStorage = new();

    public CreatureAddonCache(WorldDatabase worldDatabase, IConfiguration configuration, CreatureDataCache creatureDataCache,
                              SpellManager spellManager, DB6Storage<CreatureDisplayInfoRecord> creatureDisplayInfoRecords, DB6Storage<EmotesRecord> emotesRecords,
                              DB6Storage<AnimKitRecord> animKitRecords)
    {
        _worldDatabase = worldDatabase;
        _configuration = configuration;
        _creatureDataCache = creatureDataCache;
        _spellManager = spellManager;
        _creatureDisplayInfoRecords = creatureDisplayInfoRecords;
        _emotesRecords = emotesRecords;
        _animKitRecords = animKitRecords;
    }

    public void Load()
    {
        var time = Time.MSTime;
        //                                         0     1        2      3           4         5         6            7         8      9          10               11            12                      13
        var result = _worldDatabase.Query("SELECT guid, path_id, mount, StandState, AnimTier, VisFlags, SheathState, PvPFlags, emote, aiAnimKit, movementAnimKit, meleeAnimKit, visibilityDistanceType, auras FROM creature_addon");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature addon definitions. DB table `creature_addon` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var guid = result.Read<ulong>(0);
            var creData = _creatureDataCache.GetCreatureData(guid);

            if (creData == null)
            {
                if (_configuration.GetDefaultValue("load:autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM creature_addon WHERE guid = {guid}");
                else
                    Log.Logger.Error($"Creature (GUID: {guid}) does not exist but has a record in `creatureaddon`");

                continue;
            }

            CreatureAddon creatureAddon = new()
            {
                PathId = result.Read<uint>(1)
            };

            if (creData.MovementType == (byte)MovementGeneratorType.Waypoint && creatureAddon.PathId == 0)
            {
                creData.MovementType = (byte)MovementGeneratorType.Idle;
                Log.Logger.Error($"Creature (GUID {guid}) has movement type set to WAYPOINTMOTIONTYPE but no path assigned");
            }

            creatureAddon.Mount = result.Read<uint>(2);
            creatureAddon.StandState = result.Read<byte>(3);
            creatureAddon.AnimTier = result.Read<byte>(4);
            creatureAddon.VisFlags = result.Read<byte>(5);
            creatureAddon.SheathState = result.Read<byte>(6);
            creatureAddon.PvpFlags = result.Read<byte>(7);
            creatureAddon.Emote = result.Read<uint>(8);
            creatureAddon.AiAnimKit = result.Read<ushort>(9);
            creatureAddon.MovementAnimKit = result.Read<ushort>(10);
            creatureAddon.MeleeAnimKit = result.Read<ushort>(11);
            creatureAddon.VisibilityDistanceType = (VisibilityDistanceType)result.Read<byte>(12);

            var tokens = new StringArray(result.Read<string>(13), ' ');

            for (var c = 0; c < tokens.Length; ++c)
            {
                var id = tokens[c].Trim().Replace(",", "");

                if (!uint.TryParse(id, out var spellId))
                    continue;

                var additionalSpellInfo = _spellManager.GetSpellInfo(spellId);

                if (additionalSpellInfo == null)
                {
                    Log.Logger.Error($"Creature (GUID: {guid}) has wrong spell {spellId} defined in `auras` field in `creatureaddon`.");

                    continue;
                }

                if (additionalSpellInfo.HasAura(AuraType.ControlVehicle))
                    Log.Logger.Error($"Creature (GUID: {guid}) has SPELL_AURA_CONTROL_VEHICLE aura {spellId} defined in `auras` field in `creature_addon`.");

                if (creatureAddon.Auras.Contains(spellId))
                {
                    Log.Logger.Error($"Creature (GUID: {guid}) has duplicate aura (spell {spellId}) in `auras` field in `creature_addon`.");

                    continue;
                }

                if (additionalSpellInfo.Duration > 0)
                {
                    Log.Logger.Debug($"Creature (GUID: {guid}) has temporary aura (spell {spellId}) in `auras` field in `creature_addon`.");

                    continue;
                }

                creatureAddon.Auras.Add(spellId);
            }

            if (creatureAddon.Mount != 0)
                if (!_creatureDisplayInfoRecords.ContainsKey(creatureAddon.Mount))
                {
                    Log.Logger.Error($"Creature (GUID: {guid}) has invalid displayInfoId ({creatureAddon.Mount}) for mount defined in `creatureaddon`");
                    creatureAddon.Mount = 0;
                }

            if (creatureAddon.StandState >= (int)UnitStandStateType.Max)
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid unit stand state ({creatureAddon.StandState}) defined in `creature_addon`. Truncated to 0.");
                creatureAddon.StandState = 0;
            }

            if (creatureAddon.AnimTier >= (int)AnimTier.Max)
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid animation tier ({creatureAddon.AnimTier}) defined in `creature_addon`. Truncated to 0.");
                creatureAddon.AnimTier = 0;
            }

            if (creatureAddon.SheathState >= (int)SheathState.Max)
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid sheath state ({creatureAddon.SheathState}) defined in `creature_addon`. Truncated to 0.");
                creatureAddon.SheathState = 0;
            }

            // PvPFlags don't need any checking for the time being since they cover the entire range of a byte

            if (!_emotesRecords.ContainsKey(creatureAddon.Emote))
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid emote ({creatureAddon.Emote}) defined in `creatureaddon`.");
                creatureAddon.Emote = 0;
            }

            if (creatureAddon.AiAnimKit != 0 && !_animKitRecords.ContainsKey(creatureAddon.AiAnimKit))
            {
                Log.Logger.Error($"Creature (Guid: {guid}) has invalid aiAnimKit ({creatureAddon.AiAnimKit}) defined in `creature_addon`.");
                creatureAddon.AiAnimKit = 0;
            }

            if (creatureAddon.MovementAnimKit != 0 && !_animKitRecords.ContainsKey(creatureAddon.MovementAnimKit))
            {
                Log.Logger.Error($"Creature (Guid: {guid}) has invalid movementAnimKit ({creatureAddon.MovementAnimKit}) defined in `creature_addon`.");
                creatureAddon.MovementAnimKit = 0;
            }

            if (creatureAddon.MeleeAnimKit != 0 && !_animKitRecords.ContainsKey(creatureAddon.MeleeAnimKit))
            {
                Log.Logger.Error($"Creature (Guid: {guid}) has invalid meleeAnimKit ({creatureAddon.MeleeAnimKit}) defined in `creature_addon`.");
                creatureAddon.MeleeAnimKit = 0;
            }

            if (creatureAddon.VisibilityDistanceType >= VisibilityDistanceType.Max)
            {
                Log.Logger.Error($"Creature (GUID: {guid}) has invalid visibilityDistanceType ({creatureAddon.VisibilityDistanceType}) defined in `creature_addon`.");
                creatureAddon.VisibilityDistanceType = VisibilityDistanceType.Normal;
            }

            _creatureAddonStorage.Add(guid, creatureAddon);
            count++;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} creature addons in {Time.GetMSTimeDiffToNow(time)} ms");
    }

    public CreatureAddon GetCreatureAddon(ulong lowguid)
    {
        return _creatureAddonStorage.LookupByKey(lowguid);
    }
}