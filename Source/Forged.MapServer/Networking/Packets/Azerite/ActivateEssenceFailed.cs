// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Azerite;

internal class ActivateEssenceFailed : ServerPacket
{
    public uint Arg;
    public uint AzeriteEssenceID;
    public AzeriteEssenceActivateResult Reason;
    public byte? Slot;
    public ActivateEssenceFailed() : base(ServerOpcodes.ActivateEssenceFailed) { }

    public override void Write()
    {
        WorldPacket.WriteBits((int)Reason, 4);
        WorldPacket.WriteBit(Slot.HasValue);
        WorldPacket.WriteUInt32(Arg);
        WorldPacket.WriteUInt32(AzeriteEssenceID);

        if (Slot.HasValue)
            WorldPacket.WriteUInt8(Slot.Value);
    }
}