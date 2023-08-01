// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class CreatureBaseStatsCache : IObjectCache
{
    private readonly Dictionary<uint, CreatureBaseStats> _creatureBaseStatsStorage = new();
    private readonly CreatureTemplateCache _creatureTemplate;
    private readonly WorldDatabase _worldDatabase;

    public CreatureBaseStatsCache(CreatureTemplateCache creatureTemplate, WorldDatabase worldDatabase)
    {
        _creatureTemplate = creatureTemplate;
        _worldDatabase = worldDatabase;
    }

    public CreatureBaseStats GetCreatureBaseStats(uint level, uint unitClass)
    {
        return _creatureBaseStatsStorage.TryGetValue(MathFunctions.MakePair16(level, unitClass), out var stats) ? stats : new DefaultCreatureBaseStats();
    }

    public void Load()
    {
        var time = Time.MSTime;

        _creatureBaseStatsStorage.Clear();

        //                                         0      1      2         3            4
        var result = _worldDatabase.Query("SELECT level, class, basemana, attackpower, rangedattackpower FROM creature_classlevelstats");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 creature base stats. DB table `creature_classlevelstats` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var level = result.Read<byte>(0);
            var @class = result.Read<byte>(1);

            if (@class == 0 || (1 << @class - 1 & (int)PlayerClass.ClassMaskAllCreatures) == 0)
                Log.Logger.Error("Creature base stats for level {0} has invalid class {1}", level, @class);

            CreatureBaseStats stats = new()
            {
                BaseMana = result.Read<uint>(2),
                AttackPower = result.Read<ushort>(3),
                RangedAttackPower = result.Read<ushort>(4)
            };

            _creatureBaseStatsStorage.Add(MathFunctions.MakePair16(level, @class), stats);

            ++count;
        } while (result.NextRow());

        foreach (var creatureTemplate in _creatureTemplate.CreatureTemplates.Values)
            for (var lvl = creatureTemplate.Minlevel; lvl <= creatureTemplate.Maxlevel; ++lvl)
                if (_creatureBaseStatsStorage.LookupByKey(MathFunctions.MakePair16((uint)lvl, creatureTemplate.UnitClass)) == null)
                    Log.Logger.Error("Missing base stats for creature class {0} level {1}", creatureTemplate.UnitClass, lvl);

        Log.Logger.Information("Loaded {0} creature base stats in {1} ms", count, Time.GetMSTimeDiffToNow(time));
    }
}