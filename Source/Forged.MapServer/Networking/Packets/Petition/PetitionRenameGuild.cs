﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Petition;

public class PetitionRenameGuild : ClientPacket
{
    public string NewGuildName;
    public ObjectGuid PetitionGuid;
    public PetitionRenameGuild(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PetitionGuid = _worldPacket.ReadPackedGuid();

        _worldPacket.ResetBitPos();
        var nameLen = _worldPacket.ReadBits<uint>(7);

        NewGuildName = _worldPacket.ReadString(nameLen);
    }
}