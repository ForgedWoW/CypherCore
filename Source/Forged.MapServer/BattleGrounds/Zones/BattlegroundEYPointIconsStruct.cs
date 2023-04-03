// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct BattlegroundEYPointIconsStruct
{
    public uint WorldStateAllianceControlledIndex;

    public uint WorldStateAllianceStatusBarIcon;

    public uint WorldStateControlIndex;

    public uint WorldStateHordeControlledIndex;

    public uint WorldStateHordeStatusBarIcon;

    public BattlegroundEYPointIconsStruct(uint worldStateControlIndex, uint worldStateAllianceControlledIndex, uint worldStateHordeControlledIndex, uint worldStateAllianceStatusBarIcon, uint worldStateHordeStatusBarIcon)
    {
        WorldStateControlIndex = worldStateControlIndex;
        WorldStateAllianceControlledIndex = worldStateAllianceControlledIndex;
        WorldStateHordeControlledIndex = worldStateHordeControlledIndex;
        WorldStateAllianceStatusBarIcon = worldStateAllianceStatusBarIcon;
        WorldStateHordeStatusBarIcon = worldStateHordeStatusBarIcon;
    }
}