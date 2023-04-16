// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Warden;

public class WardenCheckManager
{
    private static readonly byte WardenMaxLuaCheckLength = 170;
    private readonly CharacterDatabase _characterDatabase;
    private readonly Dictionary<uint, byte[]> _checkResults = new();
    private readonly List<WardenCheck> _checks = new();
    private readonly IConfiguration _configuration;
    private readonly List<ushort>[] _pools = new List<ushort>[(int)WardenCheckCategory.Max];
    private readonly WorldDatabase _worldDatabase;
    public WardenCheckManager(IConfiguration configuration, WorldDatabase worldDatabase, CharacterDatabase characterDatabase)
    {
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _characterDatabase = characterDatabase;

        for (var i = 0; i < (int)WardenCheckCategory.Max; ++i)
            _pools[i] = new List<ushort>();
    }

    public ushort MaxValidCheckId => (ushort)_checks.Count;
    public static string GetWardenCategoryCountConfig(WardenCheckCategory category) => category switch
    {
        WardenCheckCategory.Inject => "Warden.NumInjectionChecks",
        WardenCheckCategory.Lua => "Warden.NumLuaSandboxChecks",
        WardenCheckCategory.Modded => "Warden.NumClientModChecks",
        _ => "",
    };

    public static WardenCheckCategory GetWardenCheckCategory(WardenCheckType type) => type switch
    {
        WardenCheckType.Timing => WardenCheckCategory.Max,
        WardenCheckType.Driver => WardenCheckCategory.Inject,
        WardenCheckType.Proc => WardenCheckCategory.Max,
        WardenCheckType.LuaEval => WardenCheckCategory.Lua,
        WardenCheckType.Mpq => WardenCheckCategory.Modded,
        WardenCheckType.PageA => WardenCheckCategory.Inject,
        WardenCheckType.PageB => WardenCheckCategory.Inject,
        WardenCheckType.Module => WardenCheckCategory.Inject,
        WardenCheckType.Mem => WardenCheckCategory.Modded,
        _ => WardenCheckCategory.Max,
    };

    public static bool IsWardenCategoryInWorldOnly(WardenCheckCategory category) => category switch
    {
        WardenCheckCategory.Inject => false,
        WardenCheckCategory.Lua => true,
        WardenCheckCategory.Modded => false,
        _ => false,
    };

    public List<ushort> GetAvailableChecks(WardenCheckCategory category)
    {
        return _pools[(int)category];
    }

    public WardenCheck GetCheckData(ushort id)
    {
        if (id < _checks.Count)
            return _checks[id];

        return null;
    }

    public byte[] GetCheckResult(ushort id)
    {
        return _checkResults.LookupByKey(id);
    }

    public void LoadWardenChecks()
    {
        var oldMSTime = Time.MSTime;

        // Check if Warden is enabled by config before loading anything
        if (!_configuration.GetDefaultValue("Warden:Enabled", false))
        {
            Log.Logger.Information("Warden disabled, loading checks skipped.");

            return;
        }

        //                                         0   1     2     3       4        5       6    7
        var result = _worldDatabase.Query("SELECT id, type, data, result, address, length, str, comment FROM warden_checks ORDER BY id ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Warden checks. DB table `warden_checks` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<ushort>(0);
            var checkType = (WardenCheckType)result.Read<byte>(1);

            var category = GetWardenCheckCategory(checkType);

            if (category == WardenCheckCategory.Max)
            {
                Log.Logger.Error($"Warden check with id {id} lists check type {checkType} in `warden_checks`, which is not supported. Skipped.");

                continue;
            }

            if (checkType == WardenCheckType.LuaEval && id > 9999)
            {
                Log.Logger.Error($"Warden Lua check with id {id} found in `warden_checks`. Lua checks may have four-digit IDs at most. Skipped.");

                continue;
            }

            WardenCheck wardenCheck = new()
            {
                Type = checkType,
                CheckId = id
            };

            if (checkType is WardenCheckType.PageA or WardenCheckType.PageB or WardenCheckType.Driver)
                wardenCheck.Data = result.Read<byte[]>(2);

            if (checkType is WardenCheckType.Mpq or WardenCheckType.Mem)
                _checkResults.Add(id, result.Read<byte[]>(3));

            if (checkType is WardenCheckType.Mem or WardenCheckType.PageA or WardenCheckType.PageB or WardenCheckType.Proc)
                wardenCheck.Address = result.Read<uint>(4);

            if (checkType is WardenCheckType.PageA or WardenCheckType.PageB or WardenCheckType.Proc)
                wardenCheck.Length = result.Read<byte>(5);

            // PROC_CHECK support missing
            if (checkType is WardenCheckType.Mem or WardenCheckType.Mpq or WardenCheckType.LuaEval or WardenCheckType.Driver or WardenCheckType.Module)
                wardenCheck.Str = result.Read<string>(6);

            wardenCheck.Comment = result.Read<string>(7);

            if (wardenCheck.Comment.IsEmpty())
                wardenCheck.Comment = "Undocumented Check";

            if (checkType == WardenCheckType.LuaEval)
            {
                if (wardenCheck.Str.Length > WardenMaxLuaCheckLength)
                {
                    Log.Logger.Error($"Found over-long Lua check for Warden check with id {id} in `warden_checks`. Max length is {WardenMaxLuaCheckLength}. Skipped.");

                    continue;
                }

                var str = $"{id:U4}";
                wardenCheck.IdStr = str.ToCharArray();
            }

            // initialize action with default action from config, this may be overridden later
            wardenCheck.Action = (WardenActions)_configuration.GetDefaultValue("Warden:ClientCheckFailAction", 0);

            _pools[(int)category].Add(id);
            ++count;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} warden checks in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadWardenOverrides()
    {
        var oldMSTime = Time.MSTime;

        // Check if Warden is enabled by config before loading anything
        if (!_configuration.GetDefaultValue("Warden:Enabled", false))
        {
            Log.Logger.Information("Warden disabled, loading check overrides skipped.");

            return;
        }

        //                                              0         1
        var result = _characterDatabase.Query("SELECT wardenId, action FROM warden_action");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Warden action overrides. DB table `warden_action` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var checkId = result.Read<ushort>(0);
            var action = (WardenActions)result.Read<byte>(1);

            // Check if action value is in range (0-2, see WardenActions enum)
            if (action > WardenActions.Ban)
            {
                Log.Logger.Error($"Warden check override action out of range (ID: {checkId}, action: {action})");
            }
            // Check if check actually exists before accessing the CheckStore vector
            else if (checkId >= _checks.Count)
            {
                Log.Logger.Error($"Warden check action override for non-existing check (ID: {checkId}, action: {action}), skipped");
            }
            else
            {
                _checks[checkId].Action = action;
                ++count;
            }
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {count} warden action overrides in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
}