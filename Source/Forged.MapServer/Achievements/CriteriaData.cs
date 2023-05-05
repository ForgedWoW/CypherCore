// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.InteropServices;
using Framework.Constants;

namespace Forged.MapServer.Achievements;

[StructLayout(LayoutKind.Explicit)]
public class CriteriaData
{
    [FieldOffset(0)] public CriteriaDataType DataType;

    [FieldOffset(4)] public CreatureStruct Creature;

    [FieldOffset(4)] public ClassRaceStruct ClassRace;

    [FieldOffset(4)] public HealthStruct Health;

    [FieldOffset(4)] public AuraStruct Aura;

    [FieldOffset(4)] public ValueStruct Value;

    [FieldOffset(4)] public LevelStruct Level;

    [FieldOffset(4)] public GenderStruct Gender;

    [FieldOffset(4)] public MapPlayersStruct MapPlayers;

    [FieldOffset(4)] public TeamStruct TeamId;

    [FieldOffset(4)] public DrunkStruct Drunk;

    [FieldOffset(4)] public HolidayStruct Holiday;

    [FieldOffset(4)] public BgLossTeamScoreStruct BattlegroundScore;

    [FieldOffset(4)] public EquippedItemStruct EquippedItem;

    [FieldOffset(4)] public MapIdStruct MapId;

    [FieldOffset(4)] public KnownTitleStruct KnownTitle;

    [FieldOffset(4)] public GameEventStruct GameEvent;

    [FieldOffset(4)] public ItemQualityStruct itemQuality;

    [FieldOffset(4)] public RawStruct Raw;

    [FieldOffset(12)] public uint ScriptId;

    public CriteriaData(CriteriaDataType dataType, uint value1, uint value2, uint scriptId)
    {
        DataType = dataType;

        Raw.Value1 = value1;
        Raw.Value2 = value2;
        ScriptId = scriptId;
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

    #endregion Structs
}