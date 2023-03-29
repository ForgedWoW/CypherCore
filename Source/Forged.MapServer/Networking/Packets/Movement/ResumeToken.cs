// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class ResumeToken : ServerPacket
{
    public uint SequenceIndex = 1;
    public uint Reason = 1;
    public ResumeToken() : base(ServerOpcodes.ResumeToken, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(SequenceIndex);
        _worldPacket.WriteBits(Reason, 2);
        _worldPacket.FlushBits();
    }
}