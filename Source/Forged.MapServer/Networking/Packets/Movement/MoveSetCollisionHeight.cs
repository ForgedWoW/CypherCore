// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class MoveSetCollisionHeight : ServerPacket
{
    public float Height = 1.0f;
    public uint MountDisplayID;
    public ObjectGuid MoverGUID;
    public UpdateCollisionHeightReason Reason;
    public float Scale = 1.0f;
    public int ScaleDuration;
    public uint SequenceIndex;
    public MoveSetCollisionHeight() : base(ServerOpcodes.MoveSetCollisionHeight) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(MoverGUID);
        _worldPacket.WriteUInt32(SequenceIndex);
        _worldPacket.WriteFloat(Height);
        _worldPacket.WriteFloat(Scale);
        _worldPacket.WriteUInt8((byte)Reason);
        _worldPacket.WriteUInt32(MountDisplayID);
        _worldPacket.WriteInt32(ScaleDuration);
    }
}