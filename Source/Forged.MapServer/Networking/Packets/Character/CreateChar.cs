// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class CreateChar : ServerPacket
{
    public ResponseCodes Code;
    public ObjectGuid Guid;
    public CreateChar() : base(ServerOpcodes.CreateChar) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8((byte)Code);
        WorldPacket.WritePackedGuid(Guid);
    }
}