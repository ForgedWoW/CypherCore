﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Petition;

public class PetitionBuy : ClientPacket
{
    public string Title;
    public ObjectGuid Unit;
    public uint Unused910;
    public PetitionBuy(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        var titleLen = _worldPacket.ReadBits<uint>(7);

        Unit = _worldPacket.ReadPackedGuid();
        Unused910 = _worldPacket.ReadUInt32();
        Title = _worldPacket.ReadString(titleLen);
    }
}