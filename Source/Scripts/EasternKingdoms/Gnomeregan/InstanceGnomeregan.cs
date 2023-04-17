// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Gnomeregan;

internal struct GnoGameObjectIds
{
    public const uint CAVE_IN_LEFT = 146085;
    public const uint CAVE_IN_RIGHT = 146086;
    public const uint RED_ROCKET = 103820;
}

internal struct GnoCreatureIds
{
    public const uint BLASTMASTER_EMI_SHORTFUSE = 7998;
    public const uint CAVERNDEEP_AMBUSHER = 6207;
    public const uint GRUBBIS = 7361;
    public const uint VICIOUS_FALLOUT = 7079;
    public const uint CHOMPER = 6215;
    public const uint ELECTROCUTIONER = 6235;
    public const uint CROWD_PUMMELER = 6229;
    public const uint MEKGINEER = 7800;
}

internal struct DataTypes
{
    public const uint BLASTMASTER_EVENT = 0;
    public const uint VICIOUS_FALLOUT = 1;
    public const uint ELECTROCUTIONER = 2;
    public const uint CROWD_PUMMELER = 3;
    public const uint THERMAPLUGG = 4;

    public const uint MAX_ENCOUNTER = 5;

    // Additional Objects
    public const uint GO_CAVE_IN_LEFT = 6;
    public const uint GO_CAVE_IN_RIGHT = 7;
    public const uint NPC_BASTMASTER_EMI_SHORTFUSE = 8;
}

internal struct DataTypes64
{
    public const uint GO_CAVE_IN_LEFT = 0;
    public const uint GO_CAVE_IN_RIGHT = 1;
    public const uint NPC_BASTMASTER_EMI_SHORTFUSE = 2;
}

internal class InstanceGnomeregan : InstanceMapScript, IInstanceMapGetInstanceScript
{
    public InstanceGnomeregan() : base(nameof(InstanceGnomeregan), 90) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceGnomereganInstanceMapScript(map);
    }

    private class InstanceGnomereganInstanceMapScript : InstanceScript
    {
        private ObjectGuid _uiBlastmasterEmiShortfuseGUID;
        private ObjectGuid _uiCaveInLeftGUID;
        private ObjectGuid _uiCaveInRightGUID;

        public InstanceGnomereganInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("GNO");
            SetBossNumber(DataTypes.MAX_ENCOUNTER);
        }

        public override void OnCreatureCreate(Creature creature)
        {
            switch (creature.Entry)
            {
                case GnoCreatureIds.BLASTMASTER_EMI_SHORTFUSE:
                    _uiBlastmasterEmiShortfuseGUID = creature.GUID;

                    break;
            }
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            switch (go.Entry)
            {
                case DataTypes64.GO_CAVE_IN_LEFT:
                    _uiCaveInLeftGUID = go.GUID;

                    break;
                case DataTypes64.GO_CAVE_IN_RIGHT:
                    _uiCaveInRightGUID = go.GUID;

                    break;
            }
        }

        public override void OnUnitDeath(Unit unit)
        {
            var creature = unit.AsCreature;

            if (creature)
                switch (creature.Entry)
                {
                    case GnoCreatureIds.VICIOUS_FALLOUT:
                        SetBossState(DataTypes.VICIOUS_FALLOUT, EncounterState.Done);

                        break;
                    case GnoCreatureIds.ELECTROCUTIONER:
                        SetBossState(DataTypes.ELECTROCUTIONER, EncounterState.Done);

                        break;
                    case GnoCreatureIds.CROWD_PUMMELER:
                        SetBossState(DataTypes.CROWD_PUMMELER, EncounterState.Done);

                        break;
                    case GnoCreatureIds.MEKGINEER:
                        SetBossState(DataTypes.THERMAPLUGG, EncounterState.Done);

                        break;
                }
        }

        public override ObjectGuid GetGuidData(uint uiType)
        {
            switch (uiType)
            {
                case DataTypes64.GO_CAVE_IN_LEFT:              return _uiCaveInLeftGUID;
                case DataTypes64.GO_CAVE_IN_RIGHT:             return _uiCaveInRightGUID;
                case DataTypes64.NPC_BASTMASTER_EMI_SHORTFUSE: return _uiBlastmasterEmiShortfuseGUID;
            }

            return ObjectGuid.Empty;
        }
    }
}