// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps;
using Forged.MapServer.Scripting.Interfaces.IOutdoorPvP;
using Framework.Constants;
using Framework.Threading;

namespace Forged.MapServer.OutdoorPVP;

public class OutdoorPvPManager : Singleton<OutdoorPvPManager>
{
	// contains all initiated outdoor pvp events
	// used when initing / cleaning up
	readonly MultiMap<Map, OutdoorPvP> m_OutdoorPvPByMap = new();

	// maps the zone ids to an outdoor pvp event
	// used in player event handling
	readonly Dictionary<(Map map, uint zoneId), OutdoorPvP> m_OutdoorPvPMap = new();

	// Holds the outdoor PvP templates
	readonly uint[] m_OutdoorMapIds =
	{
		0, 530, 530, 530, 530, 1
	};

	readonly Dictionary<OutdoorPvPTypes, uint> m_OutdoorPvPDatas = new();

	readonly LimitedThreadTaskManager _threadTaskManager = new(ConfigMgr.GetDefaultValue("Map.ParellelUpdateTasks", 20));

	// update interval
	uint m_UpdateTimer;
	OutdoorPvPManager() { }

	public void InitOutdoorPvP()
	{
		var oldMSTime = Time.MSTime;

		//                                             0       1
		var result = DB.World.Query("SELECT TypeId, ScriptName FROM outdoorpvp_template");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 outdoor PvP definitions. DB table `outdoorpvp_template` is empty.");

			return;
		}

		uint count = 0;

		do
		{
			var typeId = (OutdoorPvPTypes)result.Read<byte>(0);

			if (Global.DisableMgr.IsDisabledFor(DisableType.OutdoorPVP, (uint)typeId, null))
				continue;

			if (typeId >= OutdoorPvPTypes.Max)
			{
				Log.Logger.Error("Invalid OutdoorPvPTypes value {0} in outdoorpvp_template; skipped.", typeId);

				continue;
			}

			m_OutdoorPvPDatas[typeId] = Global.ObjectMgr.GetScriptId(result.Read<string>(1));

			++count;
		} while (result.NextRow());

		Log.Logger.Information($"Loaded {count} outdoor PvP definitions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
	}

	public void CreateOutdoorPvPForMap(Map map)
	{
		for (var outdoorPvpType = OutdoorPvPTypes.HellfirePeninsula; outdoorPvpType < OutdoorPvPTypes.Max; ++outdoorPvpType)
		{
			if (map.Id != m_OutdoorMapIds[(int)outdoorPvpType])
				continue;

			if (!m_OutdoorPvPDatas.ContainsKey(outdoorPvpType))
			{
				Log.Logger.Error("Could not initialize OutdoorPvP object for type ID {0}; no entry in database.", outdoorPvpType);

				continue;
			}

			var pvp = Global.ScriptMgr.RunScriptRet<IOutdoorPvPGetOutdoorPvP, OutdoorPvP>(p => p.GetOutdoorPvP(map), m_OutdoorPvPDatas[outdoorPvpType], null);

			if (pvp == null)
			{
				Log.Logger.Error("Could not initialize OutdoorPvP object for type ID {0}; got NULL pointer from script.", outdoorPvpType);

				continue;
			}

			if (!pvp.SetupOutdoorPvP())
			{
				Log.Logger.Error("Could not initialize OutdoorPvP object for type ID {0}; SetupOutdoorPvP failed.", outdoorPvpType);

				continue;
			}

			m_OutdoorPvPByMap.Add(map, pvp);
		}
	}

	public void DestroyOutdoorPvPForMap(Map map)
	{
		m_OutdoorPvPByMap.Remove(map);
	}

	public void AddZone(uint zoneid, OutdoorPvP handle)
	{
		m_OutdoorPvPMap[(handle.GetMap(), zoneid)] = handle;
	}

	public void HandlePlayerEnterZone(Player player, uint zoneid)
	{
		var outdoor = GetOutdoorPvPToZoneId(player.Map, zoneid);

		if (outdoor == null)
			return;

		if (outdoor.HasPlayer(player))
			return;

		outdoor.HandlePlayerEnterZone(player, zoneid);
		Log.Logger.Debug("Player {0} entered outdoorpvp id {1}", player.GUID.ToString(), outdoor.GetTypeId());
	}

	public void HandlePlayerLeaveZone(Player player, uint zoneid)
	{
		var outdoor = GetOutdoorPvPToZoneId(player.Map, zoneid);

		if (outdoor == null)
			return;

		// teleport: remove once in removefromworld, once in updatezone
		if (!outdoor.HasPlayer(player))
			return;

		outdoor.HandlePlayerLeaveZone(player, zoneid);
		Log.Logger.Debug("Player {0} left outdoorpvp id {1}", player.GUID.ToString(), outdoor.GetTypeId());
	}

	public OutdoorPvP GetOutdoorPvPToZoneId(Map map, uint zoneid)
	{
		return m_OutdoorPvPMap.LookupByKey((map, zoneid));
	}

	public void Update(uint diff)
	{
		m_UpdateTimer += diff;

		if (m_UpdateTimer > 1000)
		{
			foreach (var (_, outdoor) in m_OutdoorPvPByMap.KeyValueList)
				_threadTaskManager.Schedule(() => outdoor.Update(m_UpdateTimer));

			_threadTaskManager.Wait();
			m_UpdateTimer = 0;
		}
	}

	public bool HandleCustomSpell(Player player, uint spellId, GameObject go)
	{
		var pvp = player.GetOutdoorPvP();

		if (pvp != null && pvp.HasPlayer(player))
			return pvp.HandleCustomSpell(player, spellId, go);

		return false;
	}

	public bool HandleOpenGo(Player player, GameObject go)
	{
		var pvp = player.GetOutdoorPvP();

		if (pvp != null && pvp.HasPlayer(player))
			return pvp.HandleOpenGo(player, go);

		return false;
	}

	public void HandleDropFlag(Player player, uint spellId)
	{
		var pvp = player.GetOutdoorPvP();

		if (pvp != null && pvp.HasPlayer(player))
			pvp.HandleDropFlag(player, spellId);
	}

	public void HandlePlayerResurrects(Player player, uint zoneid)
	{
		var pvp = player.GetOutdoorPvP();

		if (pvp != null && pvp.HasPlayer(player))
			pvp.HandlePlayerResurrects(player, zoneid);
	}

	public string GetDefenseMessage(uint zoneId, uint id, Locale locale)
	{
		var bct = CliDB.BroadcastTextStorage.LookupByKey(id);

		if (bct != null)
			return Global.DB2Mgr.GetBroadcastTextValue(bct, locale);

		Log.Logger.Error("Can not find DefenseMessage (Zone: {0}, Id: {1}). BroadcastText (Id: {2}) does not exist.", zoneId, id, id);

		return "";
	}
}