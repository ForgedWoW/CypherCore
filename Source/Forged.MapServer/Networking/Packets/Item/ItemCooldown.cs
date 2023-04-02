// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ItemCooldown : ServerPacket
{
    public uint Cooldown;
    public ObjectGuid ItemGuid;
    public uint SpellID;
    public ItemCooldown() : base(ServerOpcodes.ItemCooldown) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ItemGuid);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteUInt32(Cooldown);
    }
}