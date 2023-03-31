// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Text;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Serilog;
using Forged.RealmServer.Achievements;

namespace Forged.RealmServer;

public class CharacterDatabaseCleaner
{
    private readonly WorldConfig _worldConfig;
    private readonly WorldManager _worldManager;
    private readonly CriteriaManager _criteriaManager;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDb;

    public CharacterDatabaseCleaner(WorldConfig worldConfig, WorldManager worldManager, CriteriaManager criteriaManager,
		CharacterDatabase characterDatabase, CliDB cliDb)
	{
        _worldConfig = worldConfig;
        _worldManager = worldManager;
        _criteriaManager = criteriaManager;
        _characterDatabase = characterDatabase;
        _cliDb = cliDb;
    }

    public void CleanDatabase()
	{
		// config to disable
		if (!_worldConfig.GetBoolValue(WorldCfg.CleanCharacterDb))
			return;

		Log.Logger.Information("Cleaning character database...");

		var oldMSTime = Time.MSTime;

		var flags = (CleaningFlags)_worldManager.GetPersistentWorldVariable(WorldManager.CharacterDatabaseCleaningFlagsVarId);

		// clean up
		if (flags.HasAnyFlag(CleaningFlags.AchievementProgress))
			CleanCharacterAchievementProgress();

		if (flags.HasAnyFlag(CleaningFlags.Skills))
			CleanCharacterSkills();

		if (flags.HasAnyFlag(CleaningFlags.Talents))
			CleanCharacterTalent();

		if (flags.HasAnyFlag(CleaningFlags.Queststatus))
			CleanCharacterQuestStatus();

		// NOTE: In order to have persistentFlags be set in worldstates for the next cleanup,
		// you need to define them at least once in worldstates.
		flags &= (CleaningFlags)_worldConfig.GetIntValue(WorldCfg.PersistentCharacterCleanFlags);
		_worldManager.SetPersistentWorldVariable(WorldManager.CharacterDatabaseCleaningFlagsVarId, (int)flags);

		_worldManager.CleaningFlags = flags;

		Log.Logger.Information("Cleaned character database in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
	}

	void CheckUnique(string column, string table, CheckFor check)
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

	bool AchievementProgressCheck(uint criteria)
	{
		return _criteriaManager.GetCriteria(criteria) != null;
	}

	void CleanCharacterAchievementProgress()
	{
		CheckUnique("criteria", "character_achievement_progress", AchievementProgressCheck);
	}

	bool SkillCheck(uint skill)
	{
		return _cliDb.SkillLineStorage.ContainsKey(skill);
	}

	void CleanCharacterSkills()
	{
		CheckUnique("skill", "character_skills", SkillCheck);
	}

	bool TalentCheck(uint talent_id)
	{
		var talentInfo = _cliDb.TalentStorage.LookupByKey(talent_id);

		if (talentInfo == null)
			return false;

		return _cliDb.ChrSpecializationStorage.ContainsKey(talentInfo.SpecID);
	}

	void CleanCharacterTalent()
	{
		_characterDatabase.DirectExecute("DELETE FROM character_talent WHERE talentGroup > {0}", PlayerConst.MaxSpecializations);
		CheckUnique("talentId", "character_talent", TalentCheck);
	}

	void CleanCharacterQuestStatus()
	{
		_characterDatabase.DirectExecute("DELETE FROM character_queststatus WHERE status = 0");
	}

	delegate bool CheckFor(uint id);
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