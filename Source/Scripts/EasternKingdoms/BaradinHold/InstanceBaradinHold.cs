// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BaradinHold;

internal struct DataTypes
{
    public const uint ARGALOTH = 0;
    public const uint OCCUTHAR = 1;
    public const uint ALIZABAL = 2;
}

internal struct CreatureIds
{
    public const uint EYE_OF_OCCUTHAR = 52389;
    public const uint FOCUS_FIRE_DUMMY = 52369;
    public const uint OCCUTHAR_EYE = 52368;
}

internal struct BossIds
{
    public const uint ARGALOTH = 47120;
    public const uint OCCUTHAR = 52363;
    public const uint ALIZABAL = 55869;
}

internal struct GameObjectIds
{
    public const uint ARGALOTH_DOOR = 207619;
    public const uint OCCUTHAR_DOOR = 208953;
    public const uint ALIZABAL_DOOR = 209849;
}

[Script]
internal class InstanceBaradinHold : InstanceMapScript, IInstanceMapGetInstanceScript
{
    private static readonly DoorData[] DoorData =
    {
        new(GameObjectIds.ARGALOTH_DOOR, DataTypes.ARGALOTH, DoorType.Room), new(GameObjectIds.OCCUTHAR_DOOR, DataTypes.OCCUTHAR, DoorType.Room), new(GameObjectIds.ALIZABAL_DOOR, DataTypes.ALIZABAL, DoorType.Room)
    };

    private static readonly DungeonEncounterData[] Encounters =
    {
        new(DataTypes.ARGALOTH, 1033), new(DataTypes.OCCUTHAR, 1250), new(DataTypes.ALIZABAL, 1332)
    };

    public InstanceBaradinHold() : base(nameof(InstanceBaradinHold), 757) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceBaradinHoldInstanceMapScript(map);
    }

    private class InstanceBaradinHoldInstanceMapScript : InstanceScript
    {
        private ObjectGuid _alizabalGUID;
        private ObjectGuid _argalothGUID;
        private ObjectGuid _occutharGUID;

        public InstanceBaradinHoldInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("BH");
            SetBossNumber(3);
            LoadDoorData(DoorData);
            LoadDungeonEncounterData(Encounters);
        }

        public override void OnCreatureCreate(Creature creature)
        {
            switch (creature.Entry)
            {
                case BossIds.ARGALOTH:
                    _argalothGUID = creature.GUID;

                    break;
                case BossIds.OCCUTHAR:
                    _occutharGUID = creature.GUID;

                    break;
                case BossIds.ALIZABAL:
                    _alizabalGUID = creature.GUID;

                    break;
            }
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            switch (go.Entry)
            {
                case GameObjectIds.ARGALOTH_DOOR:
                case GameObjectIds.OCCUTHAR_DOOR:
                case GameObjectIds.ALIZABAL_DOOR:
                    AddDoor(go, true);

                    break;
            }
        }

        public override ObjectGuid GetGuidData(uint data)
        {
            switch (data)
            {
                case DataTypes.ARGALOTH:
                    return _argalothGUID;
                case DataTypes.OCCUTHAR:
                    return _occutharGUID;
                case DataTypes.ALIZABAL:
                    return _alizabalGUID;
            }

            return ObjectGuid.Empty;
        }

        public override void OnGameObjectRemove(GameObject go)
        {
            switch (go.Entry)
            {
                case GameObjectIds.ARGALOTH_DOOR:
                case GameObjectIds.OCCUTHAR_DOOR:
                case GameObjectIds.ALIZABAL_DOOR:
                    AddDoor(go, false);

                    break;
            }
        }
    }
}