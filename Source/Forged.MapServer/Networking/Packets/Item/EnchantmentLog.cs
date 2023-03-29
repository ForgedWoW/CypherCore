// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class EnchantmentLog : ServerPacket
{
    public ObjectGuid Owner;
    public ObjectGuid Caster;
    public ObjectGuid ItemGUID;
    public uint ItemID;
    public uint Enchantment;
    public uint EnchantSlot;
    public EnchantmentLog() : base(ServerOpcodes.EnchantmentLog, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Owner);
        _worldPacket.WritePackedGuid(Caster);
        _worldPacket.WritePackedGuid(ItemGUID);
        _worldPacket.WriteUInt32(ItemID);
        _worldPacket.WriteUInt32(Enchantment);
        _worldPacket.WriteUInt32(EnchantSlot);
    }
}