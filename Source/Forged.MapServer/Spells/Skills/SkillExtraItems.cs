// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Spells.Skills;

public class SkillExtraItems
{
	static readonly Dictionary<uint, SkillExtraItemEntry> SkillExtraItemStorage = new();

	// loads the extra item creation info from DB
	public static void LoadSkillExtraItemTable()
	{
		var oldMSTime = Time.MSTime;

		SkillExtraItemStorage.Clear(); // need for reload

		//                                             0               1                       2                    3
		var result = DB.World.Query("SELECT spellId, requiredSpecialization, additionalCreateChance, additionalMaxNum FROM skill_extra_item_template");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 spell specialization definitions. DB table `skill_extra_item_template` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var spellId = result.Read<uint>(0);

			if (!Global.SpellMgr.HasSpellInfo(spellId, Framework.Constants.Difficulty.None))
			{
				Log.Logger.Error("Skill specialization {0} has non-existent spell id in `skill_extra_item_template`!", spellId);

				continue;
			}

			var requiredSpecialization = result.Read<uint>(1);

			if (!Global.SpellMgr.HasSpellInfo(requiredSpecialization, Framework.Constants.Difficulty.None))
			{
				Log.Logger.Error("Skill specialization {0} have not existed required specialization spell id {1} in `skill_extra_item_template`!", spellId, requiredSpecialization);

				continue;
			}

			var additionalCreateChance = result.Read<double>(2);

			if (additionalCreateChance <= 0.0f)
			{
				Log.Logger.Error("Skill specialization {0} has too low additional create chance in `skill_extra_item_template`!", spellId);

				continue;
			}

			var additionalMaxNum = result.Read<byte>(3);

			if (additionalMaxNum == 0)
			{
				Log.Logger.Error("Skill specialization {0} has 0 max number of extra items in `skill_extra_item_template`!", spellId);

				continue;
			}

			SkillExtraItemEntry skillExtraItemEntry = new()
			{
				RequiredSpecialization = requiredSpecialization,
				AdditionalCreateChance = additionalCreateChance,
				AdditionalMaxNum = additionalMaxNum
			};

			SkillExtraItemStorage[spellId] = skillExtraItemEntry;
			++count;
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} spell specialization definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public static bool CanCreateExtraItems(Player player, uint spellId, ref double additionalChance, ref byte additionalMax)
	{
		// get the info for the specified spell
		var specEntry = SkillExtraItemStorage.LookupByKey(spellId);

		if (specEntry == null)
			return false;

		// the player doesn't have the required specialization, return false
		if (!player.HasSpell(specEntry.RequiredSpecialization))
			return false;

		// set the arguments to the appropriate values
		additionalChance = specEntry.AdditionalCreateChance;
		additionalMax = specEntry.AdditionalMaxNum;

		// enable extra item creation
		return true;
	}
}

// struct to store information about perfection procs
// one entry per spell