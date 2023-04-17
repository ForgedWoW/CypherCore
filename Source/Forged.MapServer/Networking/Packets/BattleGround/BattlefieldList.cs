// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class BattlefieldList : ServerPacket
{
    public List<int> Battlefields = new();
    public ObjectGuid BattlemasterGuid;
    public int BattlemasterListID;
    public bool HasRandomWinToday;
    public byte MaxLevel;

    public byte MinLevel;

    // Players cannot join a specific Battleground instance anymore - this is always empty
    public bool PvpAnywhere;
    public BattlefieldList() : base(ServerOpcodes.BattlefieldList) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(BattlemasterGuid);
        WorldPacket.WriteInt32(BattlemasterListID);
        WorldPacket.WriteUInt8(MinLevel);
        WorldPacket.WriteUInt8(MaxLevel);
        WorldPacket.WriteInt32(Battlefields.Count);

        foreach (var field in Battlefields)
            WorldPacket.WriteInt32(field);

        WorldPacket.WriteBit(PvpAnywhere);
        WorldPacket.WriteBit(HasRandomWinToday);
        WorldPacket.FlushBits();
    }
}