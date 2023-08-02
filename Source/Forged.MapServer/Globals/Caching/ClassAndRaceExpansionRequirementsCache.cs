// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.C;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class ClassAndRaceExpansionRequirementsCache : IObjectCache
{
    private readonly DB6Storage<AchievementRecord> _achievementRecords;
    private readonly DB6Storage<ChrClassesRecord> _chrClassesRecords;
    private readonly DB6Storage<ChrRacesRecord> _chrRacesRecords;
    private readonly WorldDatabase _worldDatabase;

    public ClassAndRaceExpansionRequirementsCache(WorldDatabase worldDatabase, DB6Storage<ChrRacesRecord> chrRacesRecords, DB6Storage<AchievementRecord> achievementRecords,
                                                  DB6Storage<ChrClassesRecord> chrClassesRecords)
    {
        _worldDatabase = worldDatabase;
        _chrRacesRecords = chrRacesRecords;
        _achievementRecords = achievementRecords;
        _chrClassesRecords = chrClassesRecords;
    }

    public List<RaceClassAvailability> ClassExpansionRequirements { get; } = new();
    public Dictionary<byte, RaceUnlockRequirement> RaceUnlockRequirements { get; } = new();

    public ClassAvailability GetClassExpansionRequirement(Race raceId, PlayerClass classId)
    {
        var raceClassAvailability = ClassExpansionRequirements.Find(raceClass => raceClass.RaceID == (byte)raceId);

        var classAvailability = raceClassAvailability?.Classes.Find(availability => availability.ClassID == (byte)classId);

        return classAvailability;
    }

    public ClassAvailability GetClassExpansionRequirementFallback(byte classId)
    {
        foreach (var raceClassAvailability in ClassExpansionRequirements)
            foreach (var classAvailability in raceClassAvailability.Classes.Where(classAvailability => classAvailability.ClassID == classId))
                return classAvailability;

        return null;
    }

    public RaceUnlockRequirement GetRaceUnlockRequirement(Race race)
    {
        return RaceUnlockRequirements.LookupByKey((byte)race);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;
        RaceUnlockRequirements.Clear();

        //                                         0       1          2
        var result = _worldDatabase.Query("SELECT raceID, expansion, achievementId FROM `race_unlock_requirement`");

        if (!result.IsEmpty())
        {
            uint count = 0;

            do
            {
                var raceID = result.Read<byte>(0);
                var expansion = result.Read<byte>(1);
                var achievementId = result.Read<uint>(2);

                if (!_chrRacesRecords.TryGetValue(raceID, out _))
                {
                    Log.Logger.Error("Race {0} defined in `race_unlock_requirement` does not exists, skipped.", raceID);

                    continue;
                }

                if (expansion >= (int)Expansion.MaxAccountExpansions)
                {
                    Log.Logger.Error("Race {0} defined in `race_unlock_requirement` has incorrect expansion {1}, skipped.", raceID, expansion);

                    continue;
                }

                if (achievementId != 0 && !_achievementRecords.ContainsKey(achievementId))
                {
                    Log.Logger.Error($"Race {raceID} defined in `race_unlock_requirement` has incorrect achievement {achievementId}, skipped.");

                    continue;
                }

                RaceUnlockRequirement raceUnlockRequirement = new()
                {
                    Expansion = expansion,
                    AchievementId = achievementId
                };

                RaceUnlockRequirements[raceID] = raceUnlockRequirement;

                ++count;
            } while (result.NextRow());

            Log.Logger.Information("Loaded {0} race expansion requirements in {1} ms.", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }
        else
            Log.Logger.Information("Loaded 0 race expansion requirements. DB table `race_expansion_requirement` is empty.");

        oldMSTime = Time.MSTime;
        ClassExpansionRequirements.Clear();

        //                               0        1       2                     3
        result = _worldDatabase.Query("SELECT ClassID, RaceID, ActiveExpansionLevel, AccountExpansionLevel FROM `class_expansion_requirement`");

        if (!result.IsEmpty())
        {
            Dictionary<byte, Dictionary<byte, Tuple<byte, byte>>> temp = new();
            var minRequirementForClass = new byte[(int)PlayerClass.Max];
            Array.Fill(minRequirementForClass, (byte)Expansion.Max);
            uint count = 0;

            do
            {
                var classID = result.Read<byte>(0);
                var raceID = result.Read<byte>(1);
                var activeExpansionLevel = result.Read<byte>(2);
                var accountExpansionLevel = result.Read<byte>(3);

                if (!_chrClassesRecords.TryGetValue(classID, out _))
                {
                    Log.Logger.Error($"Class {classID} (race {raceID}) defined in `class_expansion_requirement` does not exists, skipped.");

                    continue;
                }

                if (!_chrRacesRecords.TryGetValue(raceID, out _))
                {
                    Log.Logger.Error($"Race {raceID} (class {classID}) defined in `class_expansion_requirement` does not exists, skipped.");

                    continue;
                }

                if (activeExpansionLevel >= (int)Expansion.Max)
                {
                    Log.Logger.Error($"Class {classID} Race {raceID} defined in `class_expansion_requirement` has incorrect ActiveExpansionLevel {activeExpansionLevel}, skipped.");

                    continue;
                }

                if (accountExpansionLevel >= (int)Expansion.MaxAccountExpansions)
                {
                    Log.Logger.Error($"Class {classID} Race {raceID} defined in `class_expansion_requirement` has incorrect AccountExpansionLevel {accountExpansionLevel}, skipped.");

                    continue;
                }

                if (!temp.ContainsKey(raceID))
                    temp[raceID] = new Dictionary<byte, Tuple<byte, byte>>();

                temp[raceID][classID] = Tuple.Create(activeExpansionLevel, accountExpansionLevel);
                minRequirementForClass[classID] = Math.Min(minRequirementForClass[classID], activeExpansionLevel);

                ++count;
            } while (result.NextRow());

            foreach (var race in temp)
            {
                RaceClassAvailability raceClassAvailability = new()
                {
                    RaceID = race.Key
                };

                foreach (var @class in race.Value)
                {
                    ClassAvailability classAvailability = new()
                    {
                        ClassID = @class.Key,
                        ActiveExpansionLevel = @class.Value.Item1,
                        AccountExpansionLevel = @class.Value.Item2,
                        MinActiveExpansionLevel = minRequirementForClass[@class.Key]
                    };

                    raceClassAvailability.Classes.Add(classAvailability);
                }

                ClassExpansionRequirements.Add(raceClassAvailability);
            }

            Log.Logger.Information($"Loaded {count} class expansion requirements in {Time.GetMSTimeDiffToNow(oldMSTime)} ms.");
        }
        else
            Log.Logger.Information("Loaded 0 class expansion requirements. DB table `class_expansion_requirement` is empty.");
    }
}