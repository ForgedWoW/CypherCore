// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class SuspendToken : ServerPacket
{
    public uint SequenceIndex = 1;
    public uint Reason = 1;
    public SuspendToken() : base(ServerOpcodes.SuspendToken, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(SequenceIndex);
        _worldPacket.WriteBits(Reason, 2);
        _worldPacket.FlushBits();
    }
}