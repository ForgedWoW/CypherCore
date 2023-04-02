// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Forged.MapServer.Achievements;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Tools;

internal class CharacterDatabaseCleaner
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly CriteriaManager _criteriaManager;
    private readonly SpellManager _spellManager;
    private readonly WorldManager _worldManager;

    public CharacterDatabaseCleaner(IConfiguration configuration, CharacterDatabase characterDatabase, CriteriaManager criteriaManager, CliDB cliDB, SpellManager spellManager, WorldManager worldManager)
    {
        _configuration = configuration;
        _characterDatabase = characterDatabase;
        _criteriaManager = criteriaManager;
        _cliDB = cliDB;
        _spellManager = spellManager;
        _worldManager = worldManager;
    }

    private delegate bool CheckFor(uint id);

    public void CleanDatabase()
    {
        // config to disable
        if (!_configuration.GetDefaultValue("CleanCharacterDB", false))
            return;

        Log.Logger.Information("Cleaning character database...");

        var oldMSTime = Time.MSTime;

        var flags = (CleaningFlags)_worldManager.GetPersistentWorldVariable(WorldManager.CHARACTER_DATABASE_CLEANING_FLAGS_VAR_ID);

        // clean up
        if (flags.HasAnyFlag(CleaningFlags.AchievementProgress))
            CleanCharacterAchievementProgress();

        if (flags.HasAnyFlag(CleaningFlags.Skills))
            CleanCharacterSkills();

        if (flags.HasAnyFlag(CleaningFlags.Spells))
            CleanCharacterSpell();

        if (flags.HasAnyFlag(CleaningFlags.Talents))
            CleanCharacterTalent();

        if (flags.HasAnyFlag(CleaningFlags.Queststatus))
            CleanCharacterQuestStatus();

        // NOTE: In order to have persistentFlags be set in worldstates for the next cleanup,
        // you need to define them at least once in worldstates.
        flags &= (CleaningFlags)_configuration.GetDefaultValue("PersistentCharacterCleanFlags", 0);
        _worldManager.SetPersistentWorldVariable(WorldManager.CHARACTER_DATABASE_CLEANING_FLAGS_VAR_ID, (int)flags);

        _worldManager.CleaningFlags = flags;

        Log.Logger.Information("Cleaned character database in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
    }

    private bool AchievementProgressCheck(uint criteria)
    {
        return _criteriaManager.GetCriteria(criteria) != null;
    }

    private void CheckUnique(string column, string table, CheckFor check)
    {
        var result = _characterDatabase.Query("SELECT DISTINCT {0} FROM {1}", column, table);

        if (result.IsEmpty())
        {
            Log.Logger.Information("Table {0} is empty.", table);

            return;
        }

        var found = false;
        StringBuilder ss = new();

        do
        {
            var id = result.Read<uint>(0);

            if (!check(id))
            {
                if (!found)
                {
                    ss.AppendFormat("DELETE FROM {0} WHERE {1} IN(", table, column);
                    found = true;
                }
                else
                {
                    ss.Append(',');
                }

                ss.Append(id);
            }
        } while (result.NextRow());

        if (found)
        {
            ss.Append(')');
            _characterDatabase.Execute(ss.ToString());
        }
    }
    private void CleanCharacterAchievementProgress()
    {
        CheckUnique("criteria", "character_achievement_progress", AchievementProgressCheck);
    }

    private void CleanCharacterQuestStatus()
    {
        _characterDatabase.DirectExecute("DELETE FROM character_queststatus WHERE status = 0");
    }

    private void CleanCharacterSkills()
    {
        CheckUnique("skill", "character_skills", SkillCheck);
    }

    private void CleanCharacterSpell()
    {
        CheckUnique("spell", "character_spell", SpellCheck);
    }

    private void CleanCharacterTalent()
    {
        _characterDatabase.DirectExecute("DELETE FROM character_talent WHERE talentGroup > {0}", PlayerConst.MaxSpecializations);
        CheckUnique("talentId", "character_talent", TalentCheck);
    }

    private bool SkillCheck(uint skill)
    {
        return _cliDB.SkillLineStorage.ContainsKey(skill);
    }
    private bool SpellCheck(uint spellID)
    {
        var spellInfo = _spellManager.GetSpellInfo(spellID);

        return spellInfo != null && !spellInfo.HasAttribute(SpellCustomAttributes.IsTalent);
    }
    private bool TalentCheck(uint talentID)
    {
        var talentInfo = _cliDB.TalentStorage.LookupByKey(talentID);

        if (talentInfo == null)
            return false;

        return _cliDB.ChrSpecializationStorage.ContainsKey(talentInfo.SpecID);
    }
}

[Flags]
public enum CleaningFlags
{
    AchievementProgress = 0x1,
    Skills = 0x2,
    Spells = 0x4,
    Talents = 0x8,
    Queststatus = 0x10
}