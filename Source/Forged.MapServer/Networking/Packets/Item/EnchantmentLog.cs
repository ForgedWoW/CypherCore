// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class EnchantmentLog : ServerPacket
{
    public ObjectGuid Caster;
    public uint Enchantment;
    public uint EnchantSlot;
    public ObjectGuid ItemGUID;
    public uint ItemID;
    public ObjectGuid Owner;
    public EnchantmentLog() : base(ServerOpcodes.EnchantmentLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Owner);
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WritePackedGuid(ItemGUID);
        WorldPacket.WriteUInt32(ItemID);
        WorldPacket.WriteUInt32(Enchantment);
        WorldPacket.WriteUInt32(EnchantSlot);
    }
}