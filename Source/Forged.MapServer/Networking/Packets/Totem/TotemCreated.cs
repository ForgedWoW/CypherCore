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
        _worldPacket.WriteUInt8(Slot);
        _worldPacket.WritePackedGuid(Totem);
        _worldPacket.WriteUInt32(Duration);
        _worldPacket.WriteUInt32(SpellID);
        _worldPacket.WriteFloat(TimeMod);
        _worldPacket.WriteBit(CannotDismiss);
        _worldPacket.FlushBits();
    }
}