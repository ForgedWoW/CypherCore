using System.Collections.Generic;
using System.Runtime.InteropServices;
using Framework.Constants;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Scripting.Interfaces.IAchievement;
using Game.Spells;

namespace Game.Achievements;

[StructLayout(LayoutKind.Explicit)]
public class CriteriaData
{
	[FieldOffset(0)]
	public CriteriaDataType DataType;

	[FieldOffset(4)]
	public CreatureStruct Creature;

	[FieldOffset(4)]
	public ClassRaceStruct ClassRace;

	[FieldOffset(4)]
	public HealthStruct Health;

	[FieldOffset(4)]
	public AuraStruct Aura;

	[FieldOffset(4)]
	public ValueStruct Value;

	[FieldOffset(4)]
	public LevelStruct Level;

	[FieldOffset(4)]
	public GenderStruct Gender;

	[FieldOffset(4)]
	public MapPlayersStruct MapPlayers;

	[FieldOffset(4)]
	public TeamStruct TeamId;

	[FieldOffset(4)]
	public DrunkStruct Drunk;

	[FieldOffset(4)]
	public HolidayStruct Holiday;

	[FieldOffset(4)]
	public BgLossTeamScoreStruct BattlegroundScore;

	[FieldOffset(4)]
	public EquippedItemStruct EquippedItem;

	[FieldOffset(4)]
	public MapIdStruct MapId;

	[FieldOffset(4)]
	public KnownTitleStruct KnownTitle;

	[FieldOffset(4)]
	public GameEventStruct GameEvent;

	[FieldOffset(4)]
	public ItemQualityStruct itemQuality;

	[FieldOffset(4)]
	public RawStruct Raw;

	[FieldOffset(12)]
	public uint ScriptId;

	public CriteriaData()
	{
		DataType = CriteriaDataType.None;

		Raw.Value1 = 0;
		Raw.Value2 = 0;
		ScriptId = 0;
	}

	public CriteriaData(CriteriaDataType _dataType, uint _value1, uint _value2, uint _scriptId)
	{
		DataType = _dataType;

		Raw.Value1 = _value1;
		Raw.Value2 = _value2;
		ScriptId = _scriptId;
	}

	public bool IsValid(Criteria criteria)
	{
		if (DataType >= CriteriaDataType.Max)
		{
			Log.outError(LogFilter.Sql, "Table `criteria_data` for criteria (Entry: {0}) has wrong data type ({1}), ignored.", criteria.Id, DataType);
			return false;
		}

		switch (criteria.Entry.Type)
		{
			case CriteriaType.KillCreature:
			case CriteriaType.KillAnyCreature:
			case CriteriaType.WinBattleground:
			case CriteriaType.MaxDistFallenWithoutDying:
			case CriteriaType.CompleteQuest: // only hardcoded list
			case CriteriaType.CastSpell:
			case CriteriaType.WinAnyRankedArena:
			case CriteriaType.DoEmote:
			case CriteriaType.KillPlayer:
			case CriteriaType.WinDuel:
			case CriteriaType.GetLootByType:
			case CriteriaType.LandTargetedSpellOnTarget:
			case CriteriaType.BeSpellTarget:
			case CriteriaType.GainAura:
			case CriteriaType.EquipItemInSlot:
			case CriteriaType.RollNeed:
			case CriteriaType.RollGreed:
			case CriteriaType.TrackedWorldStateUIModified:
			case CriteriaType.EarnHonorableKill:
			case CriteriaType.CompleteDailyQuest: // only Children's Week achievements
			case CriteriaType.UseItem:            // only Children's Week achievements
			case CriteriaType.DeliveredKillingBlow:
			case CriteriaType.ReachLevel:
			case CriteriaType.Login:
			case CriteriaType.LootAnyItem:
			case CriteriaType.ObtainAnyItem:
				break;
			default:
				if (DataType != CriteriaDataType.Script)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` has data for non-supported criteria type (Entry: {0} Type: {1}), ignored.", criteria.Id, (CriteriaType)criteria.Entry.Type);
					return false;
				}
				break;
		}

		switch (DataType)
		{
			case CriteriaDataType.None:
			case CriteriaDataType.InstanceScript:
				return true;
			case CriteriaDataType.TCreature:
				if (Creature.Id == 0 || Global.ObjectMgr.GetCreatureTemplate(Creature.Id) == null)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_CREATURE ({2}) has non-existing creature id in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, Creature.Id);
					return false;
				}
				return true;
			case CriteriaDataType.TPlayerClassRace:
				if (ClassRace.ClassId == 0 && ClassRace.RaceId == 0)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_PLAYER_CLASS_RACE ({2}) must not have 0 in either value field, ignored.",
								criteria.Id, criteria.Entry.Type, DataType);
					return false;
				}
				if (ClassRace.ClassId != 0 && ((1 << (int)(ClassRace.ClassId - 1)) & (int)Class.ClassMaskAllPlayable) == 0)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_PLAYER_CLASS_RACE ({2}) has non-existing class in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, ClassRace.ClassId);
					return false;
				}
				if (ClassRace.RaceId != 0 && (SharedConst.GetMaskForRace((Race)ClassRace.RaceId) & (long)SharedConst.RaceMaskAllPlayable) == 0)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_PLAYER_CLASS_RACE ({2}) has non-existing race in value2 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, ClassRace.RaceId);
					return false;
				}
				return true;
			case CriteriaDataType.TPlayerLessHealth:
				if (Health.Percent < 1 || Health.Percent > 100)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_PLAYER_LESS_HEALTH ({2}) has wrong percent value in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, Health.Percent);
					return false;
				}
				return true;
			case CriteriaDataType.SAura:
			case CriteriaDataType.TAura:
			{
				SpellInfo spellEntry = Global.SpellMgr.GetSpellInfo(Aura.SpellId, Difficulty.None);
				if (spellEntry == null)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has wrong spell id in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, Aura.SpellId);
					return false;
				}
				if (spellEntry.Effects.Count <= Aura.EffectIndex)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has wrong spell effect index in value2 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, Aura.EffectIndex);
					return false;
				}
				if (spellEntry.GetEffect(Aura.EffectIndex).ApplyAuraName == 0)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type {2} has non-aura spell effect (ID: {3} Effect: {4}), ignores.",
								criteria.Id, criteria.Entry.Type, DataType, Aura.SpellId, Aura.EffectIndex);
					return false;
				}
				return true;
			}
			case CriteriaDataType.Value:
				if (Value.ComparisonType >= (int)ComparisionType.Max)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_VALUE ({2}) has wrong ComparisionType in value2 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, Value.ComparisonType);
					return false;
				}
				return true;
			case CriteriaDataType.TLevel:
				if (Level.Min > SharedConst.GTMaxLevel)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_LEVEL ({2}) has wrong minlevel in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, Level.Min);
					return false;
				}
				return true;
			case CriteriaDataType.TGender:
				if (Gender.Gender > (int)Framework.Constants.Gender.None)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_GENDER ({2}) has wrong gender in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, Gender.Gender);
					return false;
				}
				return true;
			case CriteriaDataType.Script:
				if (ScriptId == 0)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_SCRIPT ({2}) does not have ScriptName set, ignored.",
								criteria.Id, criteria.Entry.Type, DataType);
					return false;
				}
				return true;
			case CriteriaDataType.MapPlayerCount:
				if (MapPlayers.MaxCount <= 0)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_MAP_PLAYER_COUNT ({2}) has wrong max players count in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, MapPlayers.MaxCount);
					return false;
				}
				return true;
			case CriteriaDataType.TTeam:
				if (TeamId.Team != (int)TeamFaction.Alliance && TeamId.Team != (int)TeamFaction.Horde)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_T_TEAM ({2}) has unknown team in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, TeamId.Team);
					return false;
				}
				return true;
			case CriteriaDataType.SDrunk:
				if (Drunk.State >= 4)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_DRUNK ({2}) has unknown drunken state in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, Drunk.State);
					return false;
				}
				return true;
			case CriteriaDataType.Holiday:
				if (!CliDB.HolidaysStorage.ContainsKey(Holiday.Id))
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data`(Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_HOLIDAY ({2}) has unknown holiday in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, Holiday.Id);
					return false;
				}
				return true;
			case CriteriaDataType.GameEvent:
			{
				var events = Global.GameEventMgr.GetEventMap();
				if (GameEvent.Id < 1 || GameEvent.Id >= events.Length)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_GAME_EVENT ({2}) has unknown game_event in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, GameEvent.Id);
					return false;
				}
				return true;
			}
			case CriteriaDataType.BgLossTeamScore:
				return true; // not check correctness node indexes
			case CriteriaDataType.SEquippedItem:
				if (EquippedItem.ItemQuality >= (uint)ItemQuality.Max)
				{
					Log.outError(LogFilter.Sql, "Table `achievement_criteria_requirement` (Entry: {0} Type: {1}) for requirement ACHIEVEMENT_CRITERIA_REQUIRE_S_EQUIPED_ITEM ({2}) has unknown quality state in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, EquippedItem.ItemQuality);
					return false;
				}
				return true;
			case CriteriaDataType.MapId:
				if (!CliDB.MapStorage.ContainsKey(MapId.Id))
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_MAP_ID ({2}) contains an unknown map entry in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, MapId.Id);
					return false;
				}
				return true;
			case CriteriaDataType.SPlayerClassRace:
				if (ClassRace.ClassId == 0 && ClassRace.RaceId == 0)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_PLAYER_CLASS_RACE ({2}) must not have 0 in either value field, ignored.",
								criteria.Id, criteria.Entry.Type, DataType);
					return false;
				}
				if (ClassRace.ClassId != 0 && ((1 << (int)(ClassRace.ClassId - 1)) & (int)Class.ClassMaskAllPlayable) == 0)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_PLAYER_CLASS_RACE ({2}) has non-existing class in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, ClassRace.ClassId);
					return false;
				}
				if (ClassRace.RaceId != 0 && ((ulong)SharedConst.GetMaskForRace((Race)ClassRace.RaceId) & SharedConst.RaceMaskAllPlayable) == 0)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_PLAYER_CLASS_RACE ({2}) has non-existing race in value2 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, ClassRace.RaceId);
					return false;
				}
				return true;
			case CriteriaDataType.SKnownTitle:
				if (!CliDB.CharTitlesStorage.ContainsKey(KnownTitle.Id))
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_KNOWN_TITLE ({2}) contains an unknown title_id in value1 ({3}), ignore.",
								criteria.Id, criteria.Entry.Type, DataType, KnownTitle.Id);
					return false;
				}
				return true;
			case CriteriaDataType.SItemQuality:
				if (itemQuality.Quality >= (uint)ItemQuality.Max)
				{
					Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) for data type CRITERIA_DATA_TYPE_S_ITEM_QUALITY ({2}) contains an unknown quality state value in value1 ({3}), ignored.",
								criteria.Id, criteria.Entry.Type, DataType, itemQuality.Quality);
					return false;
				}
				return true;
			default:
				Log.outError(LogFilter.Sql, "Table `criteria_data` (Entry: {0} Type: {1}) contains data of a non-supported data type ({2}), ignored.", criteria.Id, criteria.Entry.Type, DataType);
				return false;
		}
	}

	public bool Meets(uint criteriaId, Player source, WorldObject target, uint miscValue1 = 0, uint miscValue2 = 0)
	{
		switch (DataType)
		{
			case CriteriaDataType.None:
				return true;
			case CriteriaDataType.TCreature:
				if (target == null || !target.IsTypeId(TypeId.Unit))
					return false;
				return target.Entry == Creature.Id;
			case CriteriaDataType.TPlayerClassRace:
				if (target == null || !target.IsTypeId(TypeId.Player))
					return false;
				if (ClassRace.ClassId != 0 && ClassRace.ClassId != (uint)target.AsPlayer.Class)
					return false;
				if (ClassRace.RaceId != 0 && ClassRace.RaceId != (uint)target.AsPlayer.Race)
					return false;
				return true;
			case CriteriaDataType.SPlayerClassRace:
				if (source == null || !source.IsTypeId(TypeId.Player))
					return false;
				if (ClassRace.ClassId != 0 && ClassRace.ClassId != (uint)source.AsPlayer.Class)
					return false;
				if (ClassRace.RaceId != 0 && ClassRace.RaceId != (uint)source.AsPlayer.Race)
					return false;
				return true;
			case CriteriaDataType.TPlayerLessHealth:
				if (target == null || !target.IsTypeId(TypeId.Player))
					return false;
				return !target.AsPlayer.HealthAbovePct((int)Health.Percent);
			case CriteriaDataType.SAura:
				return source.HasAuraEffect(Aura.SpellId, (byte)Aura.EffectIndex);
			case CriteriaDataType.TAura:
			{
				if (target == null)
					return false;
				Unit unitTarget = target.AsUnit;
				if (unitTarget == null)
					return false;
				return unitTarget.HasAuraEffect(Aura.SpellId, Aura.EffectIndex);
			}
			case CriteriaDataType.Value:
				return MathFunctions.CompareValues((ComparisionType)Value.ComparisonType, miscValue1, Value.Value);
			case CriteriaDataType.TLevel:
				if (target == null)
					return false;
				return target.GetLevelForTarget(source) >= Level.Min;
			case CriteriaDataType.TGender:
			{
				if (target == null)
					return false;
				Unit unitTarget = target.AsUnit;
				if (unitTarget == null)
					return false;
				return unitTarget.Gender == (Gender)Gender.Gender;
			}
			case CriteriaDataType.Script:
			{
				Unit unitTarget = null;
				if (target)
					unitTarget = target.AsUnit;
				return Global.ScriptMgr.RunScriptRet<IAchievementCriteriaOnCheck>(p => p.OnCheck(source.AsPlayer, unitTarget.AsUnit), ScriptId);
			}
			case CriteriaDataType.MapPlayerCount:
				return source.Map.GetPlayersCountExceptGMs() <= MapPlayers.MaxCount;
			case CriteriaDataType.TTeam:
				if (target == null || !target.IsTypeId(TypeId.Player))
					return false;
				return (uint)target.AsPlayer.Team == TeamId.Team;
			case CriteriaDataType.SDrunk:
				return Player.GetDrunkenstateByValue(source.DrunkValue) >= (DrunkenState)Drunk.State;
			case CriteriaDataType.Holiday:
				return Global.GameEventMgr.IsHolidayActive((HolidayIds)Holiday.Id);
			case CriteriaDataType.GameEvent:
				return Global.GameEventMgr.IsEventActive((ushort)GameEvent.Id);
			case CriteriaDataType.BgLossTeamScore:
			{
				Battleground bg = source.Battleground;
				if (!bg)
					return false;

				int score = (int)bg.GetTeamScore(bg.GetPlayerTeam(source.GUID) == TeamFaction.Alliance ? Framework.Constants.TeamIds.Horde : Framework.Constants.TeamIds.Alliance);
				return score >= BattlegroundScore.Min && score <= BattlegroundScore.Max;
			}
			case CriteriaDataType.InstanceScript:
			{
				if (!source.IsInWorld)
					return false;
				Map map = source.Map;
				if (!map.IsDungeon)
				{
					Log.outError(LogFilter.Achievement, "Achievement system call AchievementCriteriaDataType.InstanceScript ({0}) for achievement criteria {1} for non-dungeon/non-raid map {2}",
								CriteriaDataType.InstanceScript, criteriaId, map.Id);
					return false;
				}
				InstanceScript instance = ((InstanceMap)map).GetInstanceScript();
				if (instance == null)
				{
					Log.outError(LogFilter.Achievement, "Achievement system call criteria_data_INSTANCE_SCRIPT ({0}) for achievement criteria {1} for map {2} but map does not have a instance script",
								CriteriaDataType.InstanceScript, criteriaId, map.Id);
					return false;
				}

				Unit unitTarget = null;
				if (target != null)
					unitTarget = target.AsUnit;
				return instance.CheckAchievementCriteriaMeet(criteriaId, source, unitTarget, miscValue1);
			}
			case CriteriaDataType.SEquippedItem:
			{
				Criteria entry = Global.CriteriaMgr.GetCriteria(criteriaId);

				uint itemId = entry.Entry.Type == CriteriaType.EquipItemInSlot ? miscValue2 : miscValue1;
				ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);
				if (itemTemplate == null)
					return false;
				return itemTemplate.GetBaseItemLevel() >= EquippedItem.ItemLevel && (uint)itemTemplate.GetQuality() >= EquippedItem.ItemQuality;
			}
			case CriteriaDataType.MapId:
				return source.Location.MapId == MapId.Id;
			case CriteriaDataType.SKnownTitle:
			{
				CharTitlesRecord titleInfo = CliDB.CharTitlesStorage.LookupByKey(KnownTitle.Id);
				if (titleInfo != null)
					return source && source.HasTitle(titleInfo.MaskID);

				return false;
			}
			case CriteriaDataType.SItemQuality:
			{
				ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(miscValue1);
				if (pProto == null)
					return false;
				return (uint)pProto.GetQuality() == itemQuality.Quality;
			}
			default:
				break;
		}
		return false;
	}

	#region Structs
	// criteria_data_TYPE_NONE              = 0 (no data)
	// criteria_data_TYPE_T_CREATURE        = 1
	public struct CreatureStruct
	{
		public uint Id;
	}
	// criteria_data_TYPE_T_PLAYER_CLASS_RACE = 2
	// criteria_data_TYPE_S_PLAYER_CLASS_RACE = 21
	public struct ClassRaceStruct
	{
		public uint ClassId;
		public uint RaceId;
	}
	// criteria_data_TYPE_T_PLAYER_LESS_HEALTH = 3
	public struct HealthStruct
	{
		public uint Percent;
	}
	// criteria_data_TYPE_S_AURA            = 5
	// criteria_data_TYPE_T_AURA            = 7
	public struct AuraStruct
	{
		public uint SpellId;
		public int EffectIndex;
	}
	// criteria_data_TYPE_VALUE             = 8
	public struct ValueStruct
	{
		public uint Value;
		public uint ComparisonType;
	}
	// criteria_data_TYPE_T_LEVEL           = 9
	public struct LevelStruct
	{
		public uint Min;
	}
	// criteria_data_TYPE_T_GENDER          = 10
	public struct GenderStruct
	{
		public uint Gender;
	}
	// criteria_data_TYPE_SCRIPT            = 11 (no data)
	// criteria_data_TYPE_MAP_PLAYER_COUNT  = 13
	public struct MapPlayersStruct
	{
		public uint MaxCount;
	}
	// criteria_data_TYPE_T_TEAM            = 14
	public struct TeamStruct
	{
		public uint Team;
	}
	// criteria_data_TYPE_S_DRUNK           = 15
	public struct DrunkStruct
	{
		public uint State;
	}
	// criteria_data_TYPE_HOLIDAY           = 16
	public struct HolidayStruct
	{
		public uint Id;
	}
	// criteria_data_TYPE_BG_LOSS_TEAM_SCORE= 17
	public struct BgLossTeamScoreStruct
	{
		public uint Min;
		public uint Max;
	}
	// criteria_data_INSTANCE_SCRIPT        = 18 (no data)
	// criteria_data_TYPE_S_EQUIPED_ITEM    = 19
	public struct EquippedItemStruct
	{
		public uint ItemLevel;
		public uint ItemQuality;
	}
	// criteria_data_TYPE_MAP_ID            = 20
	public struct MapIdStruct
	{
		public uint Id;
	}
	// criteria_data_TYPE_KNOWN_TITLE       = 23
	public struct KnownTitleStruct
	{
		public uint Id;
	}
	// CRITERIA_DATA_TYPE_S_ITEM_QUALITY    = 24
	public struct ItemQualityStruct
	{
		public uint Quality;
	}
	// criteria_data_TYPE_GAME_EVENT           = 25
	public struct GameEventStruct
	{
		public uint Id;
	}
	// raw
	public struct RawStruct
	{
		public uint Value1;
		public uint Value2;
	}
	#endregion
}