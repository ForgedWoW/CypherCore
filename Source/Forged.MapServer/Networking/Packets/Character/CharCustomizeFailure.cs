// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

internal class CharCustomizeFailure : ServerPacket
{
    public byte Result;
    public ObjectGuid CharGUID;
    public CharCustomizeFailure() : base(ServerOpcodes.CharCustomizeFailure) { }

    public override void Write()
    {
        _worldPacket.WriteUInt8(Result);
        _worldPacket.WritePackedGuid(CharGUID);
    }
}