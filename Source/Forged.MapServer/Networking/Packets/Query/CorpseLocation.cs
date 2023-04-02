// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class CorpseLocation : ServerPacket
{
    public int ActualMapID;
    public int MapID;
    public ObjectGuid Player;
    public Vector3 Position;
    public ObjectGuid Transport;
    public bool Valid;
    public CorpseLocation() : base(ServerOpcodes.CorpseLocation) { }

    public override void Write()
    {
        _worldPacket.WriteBit(Valid);
        _worldPacket.FlushBits();

        _worldPacket.WritePackedGuid(Player);
        _worldPacket.WriteInt32(ActualMapID);
        _worldPacket.WriteVector3(Position);
        _worldPacket.WriteInt32(MapID);
        _worldPacket.WritePackedGuid(Transport);
    }
}