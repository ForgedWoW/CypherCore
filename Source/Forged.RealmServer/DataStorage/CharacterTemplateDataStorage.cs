// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;

namespace Forged.RealmServer.DataStorage;

public class CharacterTemplateDataStorage : Singleton<CharacterTemplateDataStorage>
{
	readonly Dictionary<uint, CharacterTemplate> _characterTemplateStore = new();
	CharacterTemplateDataStorage() { }

	public void LoadCharacterTemplates()
	{
		var oldMSTime = Time.MSTime;
		_characterTemplateStore.Clear();

		MultiMap<uint, CharacterTemplateClass> characterTemplateClasses = new();
		var classesResult = DB.World.Query("SELECT TemplateId, FactionGroup, Class FROM character_template_class");

		if (!classesResult.IsEmpty())
			do
			{
				var templateId = classesResult.Read<uint>(0);
				var factionGroup = (FactionMasks)classesResult.Read<byte>(1);
				var classID = classesResult.Read<byte>(2);

				if (!((factionGroup & (FactionMasks.Player | FactionMasks.Alliance)) == (FactionMasks.Player | FactionMasks.Alliance)) &&
					!((factionGroup & (FactionMasks.Player | FactionMasks.Horde)) == (FactionMasks.Player | FactionMasks.Horde)))
				{
					Log.outError(LogFilter.Sql, "Faction group {0} defined for character template {1} in `character_template_class` is invalid. Skipped.", factionGroup, templateId);

					continue;
				}

				if (!CliDB.ChrClassesStorage.ContainsKey(classID))
				{
					Log.outError(LogFilter.Sql, "Class {0} defined for character template {1} in `character_template_class` does not exists, skipped.", classID, templateId);

					continue;
				}

				characterTemplateClasses.Add(templateId, new CharacterTemplateClass(factionGroup, classID));
			} while (classesResult.NextRow());
		else
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 character template classes. DB table `character_template_class` is empty.");

		var templates = DB.World.Query("SELECT Id, Name, Description, Level FROM character_template");

		if (templates.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 character templates. DB table `character_template` is empty.");

			return;
		}

		do
		{
			CharacterTemplate templ = new();
			templ.TemplateSetId = templates.Read<uint>(0);
			templ.Name = templates.Read<string>(1);
			templ.Description = templates.Read<string>(2);
			templ.Level = templates.Read<byte>(3);
			templ.Classes = characterTemplateClasses[templ.TemplateSetId];

			if (templ.Classes.Empty())
			{
				Log.outError(LogFilter.Sql, "Character template {0} does not have any classes defined in `character_template_class`. Skipped.", templ.TemplateSetId);

				continue;
			}

			_characterTemplateStore[templ.TemplateSetId] = templ;
		} while (templates.NextRow());

		Log.outInfo(LogFilter.ServerLoading, "Loaded {0} character templates in {1} ms.", _characterTemplateStore.Count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public Dictionary<uint, CharacterTemplate> GetCharacterTemplates()
	{
		return _characterTemplateStore;
	}

	public CharacterTemplate GetCharacterTemplate(uint templateId)
	{
		return _characterTemplateStore.LookupByKey(templateId);
	}
}