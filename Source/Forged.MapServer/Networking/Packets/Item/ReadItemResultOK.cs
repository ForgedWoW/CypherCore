// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ReadItemResultOK : ServerPacket
{
    public ObjectGuid Item;
    public ReadItemResultOK() : base(ServerOpcodes.ReadItemResultOk) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Item);
    }
}