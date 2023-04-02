// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class RequestCemeteryListResponse : ServerPacket
{
    public List<uint> CemeteryID = new();
    public bool IsGossipTriggered;
    public RequestCemeteryListResponse() : base(ServerOpcodes.RequestCemeteryListResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteBit(IsGossipTriggered);
        _worldPacket.FlushBits();

        _worldPacket.WriteInt32(CemeteryID.Count);

        foreach (var cemetery in CemeteryID)
            _worldPacket.WriteUInt32(cemetery);
    }
}