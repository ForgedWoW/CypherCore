// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class PartyCommandResult : ServerPacket
{
    public byte Command;
    public string Name;
    public byte Result;
    public uint ResultData;
    public ObjectGuid ResultGUID;
    public PartyCommandResult() : base(ServerOpcodes.PartyCommandResult) { }

    public override void Write()
    {
        WorldPacket.WriteBits(Name.GetByteCount(), 9);
        WorldPacket.WriteBits(Command, 4);
        WorldPacket.WriteBits(Result, 6);

        WorldPacket.WriteUInt32(ResultData);
        WorldPacket.WritePackedGuid(ResultGUID);
        WorldPacket.WriteString(Name);
    }
}

//Structs