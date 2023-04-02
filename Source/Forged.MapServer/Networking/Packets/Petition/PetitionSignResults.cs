// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Petition;

public class PetitionSignResults : ServerPacket
{
    public PetitionSigns Error = 0;
    public ObjectGuid Item;
    public ObjectGuid Player;
    public PetitionSignResults() : base(ServerOpcodes.PetitionSignResults) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Item);
        WorldPacket.WritePackedGuid(Player);

        WorldPacket.WriteBits(Error, 4);
        WorldPacket.FlushBits();
    }
}