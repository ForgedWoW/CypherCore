// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CharacterRenameResult : ServerPacket
{
    public ObjectGuid? Guid;
    public string Name;
    public ResponseCodes Result;
    public CharacterRenameResult() : base(ServerOpcodes.CharacterRenameResult) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8((byte)Result);
        WorldPacket.WriteBit(Guid.HasValue);
        WorldPacket.WriteBits(Name.GetByteCount(), 6);
        WorldPacket.FlushBits();

        if (Guid.HasValue)
            WorldPacket.WritePackedGuid(Guid.Value);

        WorldPacket.WriteString(Name);
    }
}