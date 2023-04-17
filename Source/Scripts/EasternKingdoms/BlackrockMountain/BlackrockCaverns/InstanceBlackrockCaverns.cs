// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns;

internal struct DataTypes
{
    // Encounter States // Boss GUIDs
    public const uint ROMOGG_BONECRUSHER = 0;
    public const uint CORLA = 1;
    public const uint KARSH_STEELBENDER = 2;
    public const uint BEAUTY = 3;
    public const uint ASCENDANT_LORD_OBSIDIUS = 4;

    // Additional Objects
    public const uint RAZ_THE_CRAZED = 5;
}

internal struct CreatureIds
{
    public const uint TWILIGHT_FLAME_CALLER = 39708;
    public const uint RAZ_THE_CRAZED = 39670;
    public const uint ROMOGG_BONECRUSHER = 39665;
}

[Script]
internal class InstanceBlackrockCaverns : InstanceMapScript, IInstanceMapGetInstanceScript
{
    private static readonly ObjectData[] CreatureData =
    {
        new(CreatureIds.RAZ_THE_CRAZED, DataTypes.RAZ_THE_CRAZED)
    };

    private static readonly DungeonEncounterData[] Encounters =
    {
        new(DataTypes.ROMOGG_BONECRUSHER, 1040), new(DataTypes.CORLA, 1038), new(DataTypes.KARSH_STEELBENDER, 1039), new(DataTypes.BEAUTY, 1037), new(DataTypes.ASCENDANT_LORD_OBSIDIUS, 1036)
    };

    public InstanceBlackrockCaverns() : base(nameof(InstanceBlackrockCaverns), 645) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceBlackrockCavernsInstanceMapScript(map);
    }

    private class InstanceBlackrockCavernsInstanceMapScript : InstanceScript
    {
        public InstanceBlackrockCavernsInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("BRC");
            SetBossNumber(5);
            LoadObjectData(CreatureData, null);
            LoadDungeonEncounterData(Encounters);
        }

        public override bool SetBossState(uint type, EncounterState state)
        {
            if (!base.SetBossState(type, state))
                return false;

            switch (type)
            {
                case DataTypes.ROMOGG_BONECRUSHER:
                case DataTypes.CORLA:
                case DataTypes.KARSH_STEELBENDER:
                case DataTypes.BEAUTY:
                case DataTypes.ASCENDANT_LORD_OBSIDIUS:
                    break;
            }

            return true;
        }
    }
}