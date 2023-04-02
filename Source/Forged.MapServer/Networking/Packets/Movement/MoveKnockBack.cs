// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class MoveKnockBack : ServerPacket
{
    public Vector2 Direction;
    public ObjectGuid MoverGUID;
    public uint SequenceIndex;
    public MoveKnockBackSpeeds Speeds;
    public MoveKnockBack() : base(ServerOpcodes.MoveKnockBack, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(MoverGUID);
        WorldPacket.WriteUInt32(SequenceIndex);
        WorldPacket.WriteVector2(Direction);
        Speeds.Write(WorldPacket);
    }
}