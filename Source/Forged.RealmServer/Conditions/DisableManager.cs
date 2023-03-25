// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Objects;

namespace Forged.RealmServer;

public class DisableManager : Singleton<DisableManager>
{
	readonly Dictionary<DisableType, Dictionary<uint, DisableData>> m_DisableMap = new();
	DisableManager() { }

	public void LoadDisables()
	{
		var oldMSTime = Time.MSTime;

		// reload case
		m_DisableMap.Clear();

		var result = DB.World.Query("SELECT sourceType, entry, flags, params_0, params_1 FROM disables");

		if (result.IsEmpty())
		{
			Log.Logger.Information("Loaded 0 disables. DB table `disables` is empty!");

			return;
		}

		uint total_count = 0;

		do
		{
			var type = (DisableType)result.Read<uint>(0);

			if (type >= DisableType.Max)
			{
				Log.Logger.Error("Invalid type {0} specified in `disables` table, skipped.", type);

				continue;
			}

			var entry = result.Read<uint>(1);
			var flags = (DisableFlags)result.Read<ushort>(2);
			var params_0 = result.Read<string>(3);
			var params_1 = result.Read<string>(4);

			DisableData data = new();
			data.flags = (ushort)flags;

			switch (type)
			{
				case DisableType.Spell:
					if (!(Global.SpellMgr.HasSpellInfo(entry, Difficulty.None) || flags.HasFlag(DisableFlags.SpellDeprecatedSpell)))
					{
						Log.Logger.Error("Spell entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

						continue;
					}

					if (flags == 0 || flags > DisableFlags.MaxSpell)
					{
						Log.Logger.Error("Disable flags for spell {0} are invalid, skipped.", entry);

						continue;
					}

					if (flags.HasFlag(DisableFlags.SpellMap))
					{
						var array = new StringArray(params_0, ',');

						for (byte i = 0; i < array.Length;)
							if (uint.TryParse(array[i++], out var id))
								data.param0.Add(id);
					}

					if (flags.HasFlag(DisableFlags.SpellArea))
					{
						var array = new StringArray(params_1, ',');

						for (byte i = 0; i < array.Length;)
							if (uint.TryParse(array[i++], out var id))
								data.param1.Add(id);
					}

					break;
				// checked later
				case DisableType.Quest:
					break;
				case DisableType.Map:
				case DisableType.LFGMap:
				{
					var mapEntry = CliDB.MapStorage.LookupByKey(entry);

					if (mapEntry == null)
					{
						Log.Logger.Error("Map entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

						continue;
					}

					var isFlagInvalid = false;

					switch (mapEntry.InstanceType)
					{
						case MapTypes.Common:
							if (flags != 0)
								isFlagInvalid = true;

							break;
						case MapTypes.Instance:
						case MapTypes.Raid:
							if (flags.HasFlag(DisableFlags.DungeonStatusHeroic) && Global.DB2Mgr.GetMapDifficultyData(entry, Difficulty.Heroic) == null)
								flags &= ~DisableFlags.DungeonStatusHeroic;

							if (flags.HasFlag(DisableFlags.DungeonStatusHeroic10Man) && Global.DB2Mgr.GetMapDifficultyData(entry, Difficulty.Raid10HC) == null)
								flags &= ~DisableFlags.DungeonStatusHeroic10Man;

							if (flags.HasFlag(DisableFlags.DungeonStatusHeroic25Man) && Global.DB2Mgr.GetMapDifficultyData(entry, Difficulty.Raid25HC) == null)
								flags &= ~DisableFlags.DungeonStatusHeroic25Man;

							if (flags == 0)
								isFlagInvalid = true;

							break;
						case MapTypes.Battleground:
						case MapTypes.Arena:
							Log.Logger.Error("Battlegroundmap {0} specified to be disabled in map case, skipped.", entry);

							continue;
					}

					if (isFlagInvalid)
					{
						Log.Logger.Error("Disable flags for map {0} are invalid, skipped.", entry);

						continue;
					}

					break;
				}
				case DisableType.Battleground:
					if (!CliDB.BattlemasterListStorage.ContainsKey(entry))
					{
						Log.Logger.Error("Battlegroundentry {0} from `disables` doesn't exist in dbc, skipped.", entry);

						continue;
					}

					if (flags != 0)
						Log.Logger.Error("Disable flags specified for Battleground{0}, useless data.", entry);

					break;
				case DisableType.OutdoorPVP:
					if (entry > (int)OutdoorPvPTypes.Max)
					{
						Log.Logger.Error("OutdoorPvPTypes value {0} from `disables` is invalid, skipped.", entry);

						continue;
					}

					if (flags != 0)
						Log.Logger.Error("Disable flags specified for outdoor PvP {0}, useless data.", entry);

					break;
				case DisableType.Criteria:
					if (Global.CriteriaMgr.GetCriteria(entry) == null)
					{
						Log.Logger.Error("Criteria entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

						continue;
					}

					if (flags != 0)
						Log.Logger.Error("Disable flags specified for Criteria {0}, useless data.", entry);

					break;
				case DisableType.VMAP:
				{
					var mapEntry = CliDB.MapStorage.LookupByKey(entry);

					if (mapEntry == null)
					{
						Log.Logger.Error("Map entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

						continue;
					}

					switch (mapEntry.InstanceType)
					{
						case MapTypes.Common:
							if (flags.HasFlag(DisableFlags.VmapAreaFlag))
								Log.Logger.Information("Areaflag disabled for world map {0}.", entry);

							if (flags.HasFlag(DisableFlags.VmapLiquidStatus))
								Log.Logger.Information("Liquid status disabled for world map {0}.", entry);

							break;
						case MapTypes.Instance:
						case MapTypes.Raid:
							if (flags.HasFlag(DisableFlags.VmapHeight))
								Log.Logger.Information("Height disabled for instance map {0}.", entry);

							if (flags.HasFlag(DisableFlags.VmapLOS))
								Log.Logger.Information("LoS disabled for instance map {0}.", entry);

							break;
						case MapTypes.Battleground:
							if (flags.HasFlag(DisableFlags.VmapHeight))
								Log.Logger.Information("Height disabled for Battlegroundmap {0}.", entry);

							if (flags.HasFlag(DisableFlags.VmapLOS))
								Log.Logger.Information("LoS disabled for Battlegroundmap {0}.", entry);

							break;
						case MapTypes.Arena:
							if (flags.HasFlag(DisableFlags.VmapHeight))
								Log.Logger.Information("Height disabled for arena map {0}.", entry);

							if (flags.HasFlag(DisableFlags.VmapLOS))
								Log.Logger.Information("LoS disabled for arena map {0}.", entry);

							break;
						default:
							break;
					}

					break;
				}
				case DisableType.MMAP:
				{
					var mapEntry = CliDB.MapStorage.LookupByKey(entry);

					if (mapEntry == null)
					{
						Log.Logger.Error("Map entry {0} from `disables` doesn't exist in dbc, skipped.", entry);

						continue;
					}

					switch (mapEntry.InstanceType)
					{
						case MapTypes.Common:
							Log.Logger.Information("Pathfinding disabled for world map {0}.", entry);

							break;
						case MapTypes.Instance:
						case MapTypes.Raid:
							Log.Logger.Information("Pathfinding disabled for instance map {0}.", entry);

							break;
						case MapTypes.Battleground:
							Log.Logger.Information("Pathfinding disabled for Battlegroundmap {0}.", entry);

							break;
						case MapTypes.Arena:
							Log.Logger.Information("Pathfinding disabled for arena map {0}.", entry);

							break;
						default:
							break;
					}

					break;
				}
				default:
					break;
			}

			if (!m_DisableMap.ContainsKey(type))
				m_DisableMap[type] = new Dictionary<uint, DisableData>();

			m_DisableMap[type].Add(entry, data);
			++total_count;
		} while (result.NextRow());

		Log.Logger.Information("Loaded {0} disables in {1} ms", total_count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void CheckQuestDisables()
	{
		if (!m_DisableMap.ContainsKey(DisableType.Quest) || m_DisableMap[DisableType.Quest].Count == 0)
		{
			Log.Logger.Information("Checked 0 quest disables.");

			return;
		}

		var oldMSTime = Time.MSTime;

		// check only quests, rest already done at startup
		foreach (var pair in m_DisableMap[DisableType.Quest])
		{
			var entry = pair.Key;

			if (Global.ObjectMgr.GetQuestTemplate(entry) == null)
			{
				Log.Logger.Error("Quest entry {0} from `disables` doesn't exist, skipped.", entry);
				m_DisableMap[DisableType.Quest].Remove(entry);

				continue;
			}

			if (pair.Value.flags != 0)
				Log.Logger.Error("Disable flags specified for quest {0}, useless data.", entry);
		}

		Log.Logger.Information("Checked {0} quest disables in {1} ms", m_DisableMap[DisableType.Quest].Count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public bool IsDisabledFor(DisableType type, uint entry, WorldObject refe, ushort flags = 0)
	{
		if (!m_DisableMap.ContainsKey(type) || m_DisableMap[type].Empty())
			return false;

		var data = m_DisableMap[type].LookupByKey(entry);

		if (data == null) // not disabled
			return false;

		switch (type)
		{
			case DisableType.Spell:
			{
				var spellFlags = (DisableFlags)data.flags;

				if (refe != null)
				{
					if ((refe.IsPlayer && spellFlags.HasFlag(DisableFlags.SpellPlayer)) ||
						(refe.IsCreature && (spellFlags.HasFlag(DisableFlags.SpellCreature) || (refe.AsUnit.IsPet && spellFlags.HasFlag(DisableFlags.SpellPet)))) ||
						(refe.IsGameObject && spellFlags.HasFlag(DisableFlags.SpellGameobject)))
					{
						if (spellFlags.HasAnyFlag(DisableFlags.SpellArenas | DisableFlags.SpellBattleGrounds))
						{
							var map = refe.Map;

							if (map != null)
							{
								if (spellFlags.HasFlag(DisableFlags.SpellArenas) && map.IsBattleArena)
									return true; // Current map is Arena and this spell is disabled here

								if (spellFlags.HasFlag(DisableFlags.SpellBattleGrounds) && map.IsBattleground)
									return true; // Current map is a Battleground and this spell is disabled here
							}
						}

						if (spellFlags.HasFlag(DisableFlags.SpellMap))
						{
							var mapIds = data.param0;

							if (mapIds.Contains(refe.Location.MapId))
								return true; // Spell is disabled on current map

							if (!spellFlags.HasFlag(DisableFlags.SpellArea))
								return false; // Spell is disabled on another map, but not this one, return false

							// Spell is disabled in an area, but not explicitly our current mapId. Continue processing.
						}

						if (spellFlags.HasFlag(DisableFlags.SpellArea))
						{
							var areaIds = data.param1;

							if (areaIds.Contains(refe.Area))
								return true; // Spell is disabled in this area

							return false; // Spell is disabled in another area, but not this one, return false
						}
						else
						{
							return true; // Spell disabled for all maps
						}
					}

					return false;
				}
				else if (spellFlags.HasFlag(DisableFlags.SpellDeprecatedSpell)) // call not from spellcast
				{
					return true;
				}
				else if (flags.HasAnyFlag((byte)DisableFlags.SpellLOS))
				{
					return spellFlags.HasFlag(DisableFlags.SpellLOS);
				}

				break;
			}
			case DisableType.Map:
			case DisableType.LFGMap:
				var player = refe.AsPlayer;

				if (player != null)
				{
					var mapEntry = CliDB.MapStorage.LookupByKey(entry);

					if (mapEntry.IsDungeon())
					{
						var disabledModes = (DisableFlags)data.flags;
						var targetDifficulty = player.GetDifficultyId(mapEntry);
						Global.DB2Mgr.GetDownscaledMapDifficultyData(entry, ref targetDifficulty);

						switch (targetDifficulty)
						{
							case Difficulty.Normal:
								return disabledModes.HasFlag(DisableFlags.DungeonStatusNormal);
							case Difficulty.Heroic:
								return disabledModes.HasFlag(DisableFlags.DungeonStatusHeroic);
							case Difficulty.Raid10HC:
								return disabledModes.HasFlag(DisableFlags.DungeonStatusHeroic10Man);
							case Difficulty.Raid25HC:
								return disabledModes.HasFlag(DisableFlags.DungeonStatusHeroic25Man);
							default:
								return false;
						}
					}
					else if (mapEntry.InstanceType == MapTypes.Common)
					{
						return true;
					}
				}

				return false;
			case DisableType.Quest:
				return true;
			case DisableType.Battleground:
			case DisableType.OutdoorPVP:
			case DisableType.Criteria:
			case DisableType.MMAP:
				return true;
			case DisableType.VMAP:
				return flags.HasAnyFlag(data.flags);
		}

		return false;
	}

	public bool IsVMAPDisabledFor(uint entry, byte flags)
	{
		return IsDisabledFor(DisableType.VMAP, entry, null, flags);
	}

	public bool IsPathfindingEnabled(uint mapId)
	{
		return _worldConfig.GetBoolValue(WorldCfg.EnableMmaps) && !Global.DisableMgr.IsDisabledFor(DisableType.MMAP, mapId, null);
	}

	public class DisableData
	{
		public ushort flags;
		public List<uint> param0 = new();
		public List<uint> param1 = new();
	}
}

public enum DisableType
{
	Spell = 0,
	Quest = 1,
	Map = 2,
	Battleground = 3,
	Criteria = 4,
	OutdoorPVP = 5,
	VMAP = 6,
	MMAP = 7,
	LFGMap = 8,
	Max = 9
}

[Flags]
public enum DisableFlags
{
	SpellPlayer = 0x01,
	SpellCreature = 0x02,
	SpellPet = 0x04,
	SpellDeprecatedSpell = 0x08,
	SpellMap = 0x10,
	SpellArea = 0x20,
	SpellLOS = 0x40,
	SpellGameobject = 0x80,
	SpellArenas = 0x100,
	SpellBattleGrounds = 0x200,
	MaxSpell = SpellPlayer | SpellCreature | SpellPet | SpellDeprecatedSpell | SpellMap | SpellArea | SpellLOS | SpellGameobject | SpellArenas | SpellBattleGrounds,

	VmapAreaFlag = 0x01,
	VmapHeight = 0x02,
	VmapLOS = 0x04,
	VmapLiquidStatus = 0x08,

	MMapPathFinding = 0x00,

	DungeonStatusNormal = 0x01,
	DungeonStatusHeroic = 0x02,

	DungeonStatusNormal10Man = 0x01,
	DungeonStatusNormal25Man = 0x02,
	DungeonStatusHeroic10Man = 0x04,
	DungeonStatusHeroic25Man = 0x08
}