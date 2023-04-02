// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Petition;

public class ServerPetitionShowList : ServerPacket
{
    public uint Price = 0;
    public ObjectGuid Unit;
    public ServerPetitionShowList() : base(ServerOpcodes.PetitionShowList) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Unit);
        WorldPacket.WriteUInt32(Price);
    }
}