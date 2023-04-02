// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class MasterLootCandidateList : ServerPacket
{
    public ObjectGuid LootObj;
    public List<ObjectGuid> Players = new();
    public MasterLootCandidateList() : base(ServerOpcodes.MasterLootCandidateList, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(LootObj);
        WorldPacket.WriteInt32(Players.Count);
        Players.ForEach(guid => WorldPacket.WritePackedGuid(guid));
    }
}