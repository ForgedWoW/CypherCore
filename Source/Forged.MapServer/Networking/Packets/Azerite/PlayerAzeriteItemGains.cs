// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Azerite;

internal class PlayerAzeriteItemGains : ServerPacket
{
    public ObjectGuid ItemGUID;
    public ulong XP;
    public PlayerAzeriteItemGains() : base(ServerOpcodes.PlayerAzeriteItemGains) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ItemGUID);
        WorldPacket.WriteUInt64(XP);
    }
}