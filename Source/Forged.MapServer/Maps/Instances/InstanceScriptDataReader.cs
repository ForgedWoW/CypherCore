// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Text.Json;
using Forged.MapServer.DataStorage;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Maps.Instances;

internal class InstanceScriptDataReader
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

    private readonly CliDB _cliDb;
    private readonly InstanceScript _instance;
    private JsonDocument _doc;

    public InstanceScriptDataReader(InstanceScript instance)
    {
        _instance = instance;
        _cliDb = instance.Instance.ClassFactory.Resolve<CliDB>();
    }

    private uint DifficultyId => (uint)_instance.Instance.DifficultyID;

    private string DifficultyName => _cliDb.DifficultyStorage.LookupByKey(_instance.Instance.DifficultyID).Name;

    private uint InstanceId => _instance.Instance.InstanceId;

    private uint MapId => _instance.Instance.Id;

    private string MapName => _instance.Instance.MapName;

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
            Log.Logger.Error($"JSON parser error {ex.Message} at {ex.LineNumber} while loading data for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}]");

            return Result.MalformedJson;
        }

        if (_doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            Log.Logger.Error($"Root JSON value is not an object for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}]");

            return Result.RootIsNotAnObject;
        }

        var result = ParseHeader();

        if (result != Result.Ok)
            return result;

        result = ParseBossStates();

        if (result != Result.Ok)
            return result;

        result = ParseAdditionalData();

        return result != Result.Ok ? result : Result.Ok;
    }

    private Result ParseAdditionalData()
    {
        if (!_doc.RootElement.TryGetProperty("AdditionalData", out var moreData))
            return Result.Ok;

        if (moreData.ValueKind != JsonValueKind.Object)
        {
            Log.Logger.Error($"Additional data is not an object for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}]");

            return Result.AdditionalDataIsNotAnObject;
        }

        foreach (var valueBase in _instance.GetPersistentScriptValues())
            if (moreData.TryGetProperty(valueBase.GetName(), out var value) && value.ValueKind != JsonValueKind.Null)
            {
                if (value.ValueKind != JsonValueKind.Number)
                {
                    Log.Logger.Error($"Additional data value for key {valueBase.GetName()} is not a number for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}]");

                    return Result.AdditionalDataUnexpectedValueType;
                }

                if (value.TryGetDouble(out var doubleValue))
                    valueBase.LoadValue(doubleValue);
                else
                    valueBase.LoadValue(value.GetInt64());
            }

        return Result.Ok;
    }

    private Result ParseBossStates()
    {
        if (!_doc.RootElement.TryGetProperty("BossStates", out var bossStates))
        {
            Log.Logger.Error($"Missing boss states for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}]");

            return Result.MissingBossStates;
        }

        if (bossStates.ValueKind != JsonValueKind.Array)
        {
            Log.Logger.Error($"Boss states is not an array for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}]");

            return Result.BossStatesIsNotAnObject;
        }

        for (var bossId = 0; bossId < bossStates.GetArrayLength(); ++bossId)
        {
            if (bossId >= _instance.GetEncounterCount())
            {
                Log.Logger.Error($"Boss states has entry for boss with higher id ({bossId}) than number of bosses ({_instance.GetEncounterCount()}) for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}]");

                return Result.UnknownBoss;
            }

            var bossState = bossStates[bossId];

            if (bossState.ValueKind != JsonValueKind.Number)
            {
                Log.Logger.Error($"Boss state for boss ({bossId}) is not a number for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}]");

                return Result.BossStateIsNotAnObject;
            }

            var state = (EncounterState)bossState.GetInt32();

            if (state is EncounterState.InProgress or EncounterState.Fail or EncounterState.Special)
                state = EncounterState.NotStarted;

            if (state < EncounterState.ToBeDecided)
                _instance.SetBossState((uint)bossId, state);
        }

        return Result.Ok;
    }

    private Result ParseHeader()
    {
        if (!_doc.RootElement.TryGetProperty("Header", out var header))
        {
            Log.Logger.Error($"Missing data header for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}]");

            return Result.MissingHeader;
        }

        if (header.GetString() != _instance.GetHeader())
        {
            Log.Logger.Error($"Incorrect data header for instance {InstanceId} [{MapId}-{MapName} | {DifficultyId}-{DifficultyName}], expected \"{_instance.GetHeader()}\" got \"{header.GetString()}\"");

            return Result.UnexpectedHeader;
        }

        return Result.Ok;
    }
}