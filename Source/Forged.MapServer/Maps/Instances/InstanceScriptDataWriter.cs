﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Framework.Constants;

namespace Game.Maps;

class InstanceScriptDataWriter
{
	readonly InstanceScript _instance;
	JsonObject _doc = new();

	public InstanceScriptDataWriter(InstanceScript instance)
	{
		_instance = instance;
	}

	public string GetString()
	{
		using var stream = new MemoryStream();

		using (var writer = new Utf8JsonWriter(stream))
		{
			_doc.WriteTo(writer);
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	public void FillData(bool withValues = true)
	{
		_doc.Add("Header", _instance.GetHeader());

		JsonArray bossStates = new();

		for (uint bossId = 0; bossId < _instance.GetEncounterCount(); ++bossId)
			bossStates.Add(JsonValue.Create((int)(withValues ? _instance.GetBossState(bossId) : EncounterState.NotStarted)));

		_doc.Add("BossStates", bossStates);

		if (!_instance.GetPersistentScriptValues().Empty())
		{
			JsonObject moreData = new();

			foreach (var additionalValue in _instance.GetPersistentScriptValues())
				if (withValues)
				{
					var data = additionalValue.CreateEvent();

					if (data.Value is double)
						moreData.Add(data.Key, (double)data.Value);
					else
						moreData.Add(data.Key, (long)data.Value);
				}
				else
				{
					moreData.Add(additionalValue.GetName(), null);
				}

			_doc.Add("AdditionalData", moreData);
		}
	}

	public void FillDataFrom(string data)
	{
		try
		{
			_doc = JsonNode.Parse(data).AsObject();
		}
		catch (JsonException)
		{
			FillData(false);
		}
	}

	public void SetBossState(UpdateBossStateSaveDataEvent data)
	{
		var array = _doc["BossStates"].AsArray();
		array[(int)data.BossId] = (int)data.NewState;
	}

	public void SetAdditionalData(UpdateAdditionalSaveDataEvent data)
	{
		var jObject = _doc["AdditionalData"].AsObject();

		if (data.Value is double)
			jObject[data.Key] = (double)data.Value;
		else
			jObject[data.Key] = (long)data.Value;
	}
}