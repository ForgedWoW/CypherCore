// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

public class MoveSetCollisionHeightAck : ClientPacket
{
    public MovementAck Data;
    public float Height = 1.0f;
    public uint MountDisplayID;
    public UpdateCollisionHeightReason Reason;
    public MoveSetCollisionHeightAck(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Data.Read(_worldPacket);
        Height = _worldPacket.ReadFloat();
        MountDisplayID = _worldPacket.ReadUInt32();
        Reason = (UpdateCollisionHeightReason)_worldPacket.ReadUInt8();
    }
}