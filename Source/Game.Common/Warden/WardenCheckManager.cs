// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.Common.Server;
using Game.Common.Warden;
using Game;

namespace Game.Common.Warden;

public class WardenCheckManager : Singleton<WardenCheckManager>
{
	static readonly byte WARDEN_MAX_LUA_CHECK_LENGTH = 170;
	readonly List<WardenCheck> _checks = new();
	readonly Dictionary<uint, byte[]> _checkResults = new();
	readonly List<ushort>[] _pools = new List<ushort>[(int)WardenCheckCategory.Max];

	public ushort MaxValidCheckId => (ushort)_checks.Count;

	WardenCheckManager()
	{
		for (var i = 0; i < (int)WardenCheckCategory.Max; ++i)
			_pools[i] = new List<ushort>();
	}

	public void LoadWardenChecks()
	{
		var oldMSTime = Time.MSTime;

		// Check if Warden is enabled by config before loading anything
		if (!WorldConfig.GetBoolValue(WorldCfg.WardenEnabled))
		{
			Log.outInfo(LogFilter.Warden, "Warden disabled, loading checks skipped.");

			return;
		}

		//                                         0   1     2     3       4        5       6    7
		var result = DB.World.Query("SELECT id, type, data, result, address, length, str, comment FROM warden_checks ORDER BY id ASC");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Warden checks. DB table `warden_checks` is empty!");

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
				Log.outError(LogFilter.Sql, $"Warden check with id {id} lists check type {checkType} in `warden_checks`, which is not supported. Skipped.");

				continue;
			}

			if ((checkType == WardenCheckType.LuaEval) && (id > 9999))
			{
				Log.outError(LogFilter.Sql, $"Warden Lua check with id {id} found in `warden_checks`. Lua checks may have four-digit IDs at most. Skipped.");

				continue;
			}

			WardenCheck wardenCheck = new();
			wardenCheck.Type = checkType;
			wardenCheck.CheckId = id;

			if (checkType == WardenCheckType.PageA || checkType == WardenCheckType.PageB || checkType == WardenCheckType.Driver)
				wardenCheck.Data = result.Read<byte[]>(2);

			if (checkType == WardenCheckType.Mpq || checkType == WardenCheckType.Mem)
				_checkResults.Add(id, result.Read<byte[]>(3));

			if (checkType == WardenCheckType.Mem || checkType == WardenCheckType.PageA || checkType == WardenCheckType.PageB || checkType == WardenCheckType.Proc)
				wardenCheck.Address = result.Read<uint>(4);

			if (checkType == WardenCheckType.PageA || checkType == WardenCheckType.PageB || checkType == WardenCheckType.Proc)
				wardenCheck.Length = result.Read<byte>(5);

			// PROC_CHECK support missing
			if (checkType == WardenCheckType.Mem || checkType == WardenCheckType.Mpq || checkType == WardenCheckType.LuaEval || checkType == WardenCheckType.Driver || checkType == WardenCheckType.Module)
				wardenCheck.Str = result.Read<string>(6);

			wardenCheck.Comment = result.Read<string>(7);

			if (wardenCheck.Comment.IsEmpty())
				wardenCheck.Comment = "Undocumented Check";

			if (checkType == WardenCheckType.LuaEval)
			{
				if (wardenCheck.Str.Length > WARDEN_MAX_LUA_CHECK_LENGTH)
				{
					Log.outError(LogFilter.Sql, $"Found over-long Lua check for Warden check with id {id} in `warden_checks`. Max length is {WARDEN_MAX_LUA_CHECK_LENGTH}. Skipped.");

					continue;
				}

				var str = $"{id:U4}";
				wardenCheck.IdStr = str.ToCharArray();
			}

			// initialize action with default action from config, this may be overridden later
			wardenCheck.Action = (WardenActions)WorldConfig.GetIntValue(WorldCfg.WardenClientFailAction);

			_pools[(int)category].Add(id);
			++count;
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} warden checks in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public void LoadWardenOverrides()
	{
		var oldMSTime = Time.MSTime;

		// Check if Warden is enabled by config before loading anything
		if (!WorldConfig.GetBoolValue(WorldCfg.WardenEnabled))
		{
			Log.outInfo(LogFilter.Warden, "Warden disabled, loading check overrides skipped.");

			return;
		}

		//                                              0         1
		var result = DB.Characters.Query("SELECT wardenId, action FROM warden_action");

		if (result.IsEmpty())
		{
			Log.outInfo(LogFilter.ServerLoading, "Loaded 0 Warden action overrides. DB table `warden_action` is empty!");

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
				Log.outError(LogFilter.Warden, $"Warden check override action out of range (ID: {checkId}, action: {action})");
			}
			// Check if check actually exists before accessing the CheckStore vector
			else if (checkId >= _checks.Count)
			{
				Log.outError(LogFilter.Warden, $"Warden check action override for non-existing check (ID: {checkId}, action: {action}), skipped");
			}
			else
			{
				_checks[checkId].Action = action;
				++count;
			}
		} while (result.NextRow());

		Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} warden action overrides in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public WardenCheck GetCheckData(ushort Id)
	{
		if (Id < _checks.Count)
			return _checks[Id];

		return null;
	}

	public byte[] GetCheckResult(ushort Id)
	{
		return _checkResults.LookupByKey(Id);
	}

	public List<ushort> GetAvailableChecks(WardenCheckCategory category)
	{
		return _pools[(int)category];
	}

	public static WardenCheckCategory GetWardenCheckCategory(WardenCheckType type) => type switch
	{
		WardenCheckType.Timing  => WardenCheckCategory.Max,
		WardenCheckType.Driver  => WardenCheckCategory.Inject,
		WardenCheckType.Proc    => WardenCheckCategory.Max,
		WardenCheckType.LuaEval => WardenCheckCategory.Lua,
		WardenCheckType.Mpq     => WardenCheckCategory.Modded,
		WardenCheckType.PageA   => WardenCheckCategory.Inject,
		WardenCheckType.PageB   => WardenCheckCategory.Inject,
		WardenCheckType.Module  => WardenCheckCategory.Inject,
		WardenCheckType.Mem     => WardenCheckCategory.Modded,
		_                       => WardenCheckCategory.Max,
	};

	public static WorldCfg GetWardenCategoryCountConfig(WardenCheckCategory category) => category switch
	{
		WardenCheckCategory.Inject => WorldCfg.WardenNumInjectChecks,
		WardenCheckCategory.Lua    => WorldCfg.WardenNumLuaChecks,
		WardenCheckCategory.Modded => WorldCfg.WardenNumClientModChecks,
		_                          => WorldCfg.Max,
	};

	public static bool IsWardenCategoryInWorldOnly(WardenCheckCategory category) => category switch
	{
		WardenCheckCategory.Inject => false,
		WardenCheckCategory.Lua    => true,
		WardenCheckCategory.Modded => false,
		_                          => false,
	};
}
