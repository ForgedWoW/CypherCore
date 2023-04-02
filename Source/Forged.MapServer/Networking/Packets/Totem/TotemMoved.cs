// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Totem;

internal class TotemMoved : ServerPacket
{
    public byte NewSlot;
    public byte Slot;
    public ObjectGuid Totem;
    public TotemMoved() : base(ServerOpcodes.TotemMoved) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8(Slot);
        WorldPacket.WriteUInt8(NewSlot);
        WorldPacket.WritePackedGuid(Totem);
    }
}