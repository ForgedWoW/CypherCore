// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Transmogification;

internal class AccountTransmogUpdate : ServerPacket
{
    public List<uint> FavoriteAppearances = new();
    public bool IsFullUpdate;
    public bool IsSetFavorite;
    public List<uint> NewAppearances = new();
    public AccountTransmogUpdate() : base(ServerOpcodes.AccountTransmogUpdate) { }

    public override void Write()
    {
        WorldPacket.WriteBit(IsFullUpdate);
        WorldPacket.WriteBit(IsSetFavorite);
        WorldPacket.WriteInt32(FavoriteAppearances.Count);
        WorldPacket.WriteInt32(NewAppearances.Count);

        foreach (var itemModifiedAppearanceId in FavoriteAppearances)
            WorldPacket.WriteUInt32(itemModifiedAppearanceId);

        foreach (var newAppearance in NewAppearances)
            WorldPacket.WriteUInt32(newAppearance);
    }
}