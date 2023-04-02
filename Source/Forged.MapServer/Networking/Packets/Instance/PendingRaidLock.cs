// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Instance;

internal class PendingRaidLock : ServerPacket
{
    public uint CompletedMask;
    public bool Extending;
    public int TimeUntilLock;
    public bool WarningOnly;
    public PendingRaidLock() : base(ServerOpcodes.PendingRaidLock) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(TimeUntilLock);
        WorldPacket.WriteUInt32(CompletedMask);
        WorldPacket.WriteBit(Extending);
        WorldPacket.WriteBit(WarningOnly);
        WorldPacket.FlushBits();
    }
}