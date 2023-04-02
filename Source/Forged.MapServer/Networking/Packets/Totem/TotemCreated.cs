// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Totem;

internal class TotemCreated : ServerPacket
{
    public bool CannotDismiss;
    public uint Duration;
    public byte Slot;
    public uint SpellID;
    public float TimeMod = 1.0f;
    public ObjectGuid Totem;
    public TotemCreated() : base(ServerOpcodes.TotemCreated) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8(Slot);
        WorldPacket.WritePackedGuid(Totem);
        WorldPacket.WriteUInt32(Duration);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteFloat(TimeMod);
        WorldPacket.WriteBit(CannotDismiss);
        WorldPacket.FlushBits();
    }
}