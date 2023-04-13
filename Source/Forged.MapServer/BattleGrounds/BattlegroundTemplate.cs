// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.BattleGrounds;

public class BattlegroundTemplate
{
    public BattlemasterListRecord BattlemasterEntry { get; set; }
    public BattlegroundTypeId Id { get; set; }
    public float MaxStartDistSq { get; set; }
    public uint ScriptId { get; set; }
    public WorldSafeLocsEntry[] StartLocation { get; set; } = new WorldSafeLocsEntry[SharedConst.PvpTeamsCount];
    public byte Weight { get; set; }

    public byte MaxLevel => BattlemasterEntry.MaxLevel;

    public ushort MaxPlayersPerTeam => (ushort)BattlemasterEntry.MaxPlayers;

    public byte MinLevel => BattlemasterEntry.MinLevel;

    public ushort MinPlayersPerTeam => (ushort)BattlemasterEntry.MinPlayers;

    public bool IsArena => BattlemasterEntry.InstanceType == (uint)MapTypes.Arena;
}