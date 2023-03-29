// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Transmogification;

internal class AccountTransmogUpdate : ServerPacket
{
    public bool IsFullUpdate;
    public bool IsSetFavorite;
    public List<uint> FavoriteAppearances = new();
    public List<uint> NewAppearances = new();
    public AccountTransmogUpdate() : base(ServerOpcodes.AccountTransmogUpdate) { }

    public override void Write()
    {
        _worldPacket.WriteBit(IsFullUpdate);
        _worldPacket.WriteBit(IsSetFavorite);
        _worldPacket.WriteInt32(FavoriteAppearances.Count);
        _worldPacket.WriteInt32(NewAppearances.Count);

        foreach (var itemModifiedAppearanceId in FavoriteAppearances)
            _worldPacket.WriteUInt32(itemModifiedAppearanceId);

        foreach (var newAppearance in NewAppearances)
            _worldPacket.WriteUInt32(newAppearance);
    }
}