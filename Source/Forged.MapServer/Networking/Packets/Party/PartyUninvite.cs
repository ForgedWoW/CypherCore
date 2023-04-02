﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class PartyUninvite : ClientPacket
{
    public byte PartyIndex;
    public string Reason;
    public ObjectGuid TargetGUID;
    public PartyUninvite(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = _worldPacket.ReadUInt8();
        TargetGUID = _worldPacket.ReadPackedGuid();

        var reasonLen = _worldPacket.ReadBits<byte>(8);
        Reason = _worldPacket.ReadString(reasonLen);
    }
}