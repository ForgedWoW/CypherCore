// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.BattleGrounds;

public class BattlegroundTemplate
{
    public BattlemasterListRecord BattlemasterEntry;
    public BattlegroundTypeId Id;
    public float MaxStartDistSq;
    public uint ScriptId;
    public WorldSafeLocsEntry[] StartLocation = new WorldSafeLocsEntry[SharedConst.PvpTeamsCount];
    public byte Weight;

    public byte GetMaxLevel()
    {
        return BattlemasterEntry.MaxLevel;
    }

    public ushort GetMaxPlayersPerTeam()
    {
        return (ushort)BattlemasterEntry.MaxPlayers;
    }

    public byte GetMinLevel()
    {
        return BattlemasterEntry.MinLevel;
    }

    public ushort GetMinPlayersPerTeam()
    {
        return (ushort)BattlemasterEntry.MinPlayers;
    }

    public bool IsArena()
    {
        return BattlemasterEntry.InstanceType == (uint)MapTypes.Arena;
    }
}