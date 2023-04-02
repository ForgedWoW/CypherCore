// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

internal class CharCustomizeFailure : ServerPacket
{
    public ObjectGuid CharGUID;
    public byte Result;
    public CharCustomizeFailure() : base(ServerOpcodes.CharCustomizeFailure) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8(Result);
        WorldPacket.WritePackedGuid(CharGUID);
    }
}