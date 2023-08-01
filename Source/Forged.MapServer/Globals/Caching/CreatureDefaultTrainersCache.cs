// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class CreatureDefaultTrainersCache : IObjectCache
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<(uint creatureId, uint gossipMenuId, uint gossipOptionIndex), uint> _creatureDefaultTrainers = new();
    private readonly CreatureTemplateCache _creatureTemplate;
    private readonly GossipMenuItemsCache _gossipMenuItemsCache;
    private readonly TrainerCache _trainerCache;
    private readonly WorldDatabase _worldDatabase;

    public CreatureDefaultTrainersCache(IConfiguration configuration, WorldDatabase worldDatabase, CreatureTemplateCache creatureTemplate,
                                        GossipMenuItemsCache gossipMenuItemsCache, TrainerCache trainerCache)
    {
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _creatureTemplate = creatureTemplate;
        _gossipMenuItemsCache = gossipMenuItemsCache;
        _trainerCache = trainerCache;
    }

    public uint GetCreatureTrainerForGossipOption(uint creatureId, uint gossipMenuId, uint gossipOptionIndex)
    {
        return _creatureDefaultTrainers.LookupByKey((creatureId, gossipMenuId, gossipOptionIndex));
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        _creatureDefaultTrainers.Clear();

        var result = _worldDatabase.Query("SELECT CreatureID, TrainerID, MenuID, OptionID FROM creature_trainer");

        if (!result.IsEmpty())
            do
            {
                var creatureId = result.Read<uint>(0);
                var trainerId = result.Read<uint>(1);
                var gossipMenuId = result.Read<uint>(2);
                var gossipOptionIndex = result.Read<uint>(3);

                if (_creatureTemplate.GetCreatureTemplate(creatureId) == null)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_trainer WHERE CreatureID = {creatureId}");
                    else
                        Log.Logger.Error($"Table `creature_trainer` references non-existing creature template (CreatureId: {creatureId}), ignoring");

                    continue;
                }

                if (_trainerCache.GetTrainer(trainerId) == null)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"DELETE FROM creature_trainer WHERE CreatureID = {creatureId}");
                    else
                        Log.Logger.Error($"Table `creature_trainer` references non-existing trainer (TrainerId: {trainerId}) for CreatureId {creatureId} MenuId {gossipMenuId} OptionIndex {gossipOptionIndex}, ignoring");

                    continue;
                }

                if (gossipMenuId != 0 || gossipOptionIndex != 0)
                {
                    var gossipMenuItems = _gossipMenuItemsCache.GetGossipMenuItemsMapBounds(gossipMenuId);
                    var gossipOptionItr = gossipMenuItems.Find(entry => entry.OrderIndex == gossipOptionIndex);

                    if (gossipOptionItr == null)
                    {
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM creature_trainer WHERE CreatureID = {creatureId}");
                        else
                            Log.Logger.Error($"Table `creature_trainer` references non-existing gossip menu option (MenuId {gossipMenuId} OptionIndex {gossipOptionIndex}) for CreatureId {creatureId} and TrainerId {trainerId}, ignoring");

                        continue;
                    }
                }

                _creatureDefaultTrainers[(creatureId, gossipMenuId, gossipOptionIndex)] = trainerId;
            } while (result.NextRow());

        Log.Logger.Information($"Loaded {_creatureDefaultTrainers.Count} default trainers in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
}