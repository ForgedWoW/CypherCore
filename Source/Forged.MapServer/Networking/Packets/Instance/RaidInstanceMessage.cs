// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Instance;

internal class RaidInstanceMessage : ServerPacket
{
    public Difficulty DifficultyID;
    public bool Extended;
    public bool Locked;
    public uint MapID;
    public InstanceResetWarningType Type;
    public RaidInstanceMessage() : base(ServerOpcodes.RaidInstanceMessage) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8((byte)Type);
        WorldPacket.WriteUInt32(MapID);
        WorldPacket.WriteUInt32((uint)DifficultyID);
        WorldPacket.WriteBit(Locked);
        WorldPacket.WriteBit(Extended);
        WorldPacket.FlushBits();
    }
}