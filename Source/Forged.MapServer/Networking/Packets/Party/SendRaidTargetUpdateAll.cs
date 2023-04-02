// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class SendRaidTargetUpdateAll : ServerPacket
{
    public sbyte PartyIndex;
    public Dictionary<byte, ObjectGuid> TargetIcons = new();
    public SendRaidTargetUpdateAll() : base(ServerOpcodes.SendRaidTargetUpdateAll) { }

    public override void Write()
    {
        WorldPacket.WriteInt8(PartyIndex);

        WorldPacket.WriteInt32(TargetIcons.Count);

        foreach (var pair in TargetIcons)
        {
            WorldPacket.WritePackedGuid(pair.Value);
            WorldPacket.WriteUInt8(pair.Key);
        }
    }
}