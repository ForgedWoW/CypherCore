// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Text.Json;
using Framework.Constants;
using Game.DataStorage;

namespace Game.Maps;

class InstanceScriptDataReader
{
	public enum Result
	{
		Ok,
		MalformedJson,
		RootIsNotAnObject,
		MissingHeader,
		UnexpectedHeader,
		MissingBossStates,
		BossStatesIsNotAnObject,
		UnknownBoss,
		BossStateIsNotAnObject,
		MissingBossState,
		BossStateValueIsNotANumber,
		AdditionalDataIsNotAnObject,
		AdditionalDataUnexpectedValueType
	}

	readonly InstanceScript _instance;
	JsonDocument _doc;

	public InstanceScriptDataReader(InstanceScript instance)
	{
		_instance = instance;
	}

	public Result Load(string data)
	{
		/*
		   Expected JSON

		    {
		        "Header": "HEADER_STRING_SET_BY_SCRIPT",
		        "BossStates": [0,2,0,...] // indexes are boss ids, values are EncounterState
		        "AdditionalData: { // optional
		            "ExtraKey1": 123
		            "AnotherExtraKey": 2.0
		        }
		    }
		*/

		try
		{
			_doc = JsonDocument.Parse(data);
		}
		catch (JsonException ex)
		{
			Log.outError(LogFilter.Scripts, $"JSON parser error {ex.Message} at {ex.LineNumber} while loading data for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");

			return Result.MalformedJson;
		}

		if (_doc.RootElement.ValueKind != JsonValueKind.Object)
		{
			Log.outError(LogFilter.Scripts, $"Root JSON value is not an object for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");

			return Result.RootIsNotAnObject;
		}

		var result = ParseHeader();

		if (result != Result.Ok)
			return result;

		result = ParseBossStates();

		if (result != Result.Ok)
			return result;

		result = ParseAdditionalData();

		if (result != Result.Ok)
			return result;

		return Result.Ok;
	}

	Result ParseHeader()
	{
		if (!_doc.RootElement.TryGetProperty("Header", out var header))
		{
			Log.outError(LogFilter.Scripts, $"Missing data header for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");

			return Result.MissingHeader;
		}

		if (header.GetString() != _instance.GetHeader())
		{
			Log.outError(LogFilter.Scripts, $"Incorrect data header for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}], expected \"{_instance.GetHeader()}\" got \"{header.GetString()}\"");

			return Result.UnexpectedHeader;
		}

		return Result.Ok;
	}

	Result ParseBossStates()
	{
		if (!_doc.RootElement.TryGetProperty("BossStates", out var bossStates))
		{
			Log.outError(LogFilter.Scripts, $"Missing boss states for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");

			return Result.MissingBossStates;
		}

		if (bossStates.ValueKind != JsonValueKind.Array)
		{
			Log.outError(LogFilter.Scripts, $"Boss states is not an array for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");

			return Result.BossStatesIsNotAnObject;
		}

		for (var bossId = 0; bossId < bossStates.GetArrayLength(); ++bossId)
		{
			if (bossId >= _instance.GetEncounterCount())
			{
				Log.outError(LogFilter.Scripts, $"Boss states has entry for boss with higher id ({bossId}) than number of bosses ({_instance.GetEncounterCount()}) for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");

				return Result.UnknownBoss;
			}

			var bossState = bossStates[bossId];

			if (bossState.ValueKind != JsonValueKind.Number)
			{
				Log.outError(LogFilter.Scripts, $"Boss state for boss ({bossId}) is not a number for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");

				return Result.BossStateIsNotAnObject;
			}

			var state = (EncounterState)bossState.GetInt32();

			if (state == EncounterState.InProgress || state == EncounterState.Fail || state == EncounterState.Special)
				state = EncounterState.NotStarted;

			if (state < EncounterState.ToBeDecided)
				_instance.SetBossState((uint)bossId, state);
		}

		return Result.Ok;
	}

	Result ParseAdditionalData()
	{
		if (!_doc.RootElement.TryGetProperty("AdditionalData", out var moreData))
			return Result.Ok;

		if (moreData.ValueKind != JsonValueKind.Object)
		{
			Log.outError(LogFilter.Scripts, $"Additional data is not an object for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");

			return Result.AdditionalDataIsNotAnObject;
		}

		foreach (var valueBase in _instance.GetPersistentScriptValues())
			if (moreData.TryGetProperty(valueBase.GetName(), out var value) && value.ValueKind != JsonValueKind.Null)
			{
				if (value.ValueKind != JsonValueKind.Number)
				{
					Log.outError(LogFilter.Scripts, $"Additional data value for key {valueBase.GetName()} is not a number for instance {GetInstanceId()} [{GetMapId()}-{GetMapName()} | {GetDifficultyId()}-{GetDifficultyName()}]");

					return Result.AdditionalDataUnexpectedValueType;
				}

				if (value.TryGetDouble(out var doubleValue))
					valueBase.LoadValue(doubleValue);
				else
					valueBase.LoadValue(value.GetInt64());
			}

		return Result.Ok;
	}

	uint GetInstanceId()
	{
		return _instance.Instance.InstanceId;
	}

	uint GetMapId()
	{
		return _instance.Instance.Id;
	}

	string GetMapName()
	{
		return _instance.Instance.MapName;
	}

	uint GetDifficultyId()
	{
		return (uint)_instance.Instance.DifficultyID;
	}

	string GetDifficultyName()
	{
		return CliDB.DifficultyStorage.LookupByKey(_instance.Instance.DifficultyID).Name;
	}
}