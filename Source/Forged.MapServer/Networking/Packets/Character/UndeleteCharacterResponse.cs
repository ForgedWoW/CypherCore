// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Character;

public class UndeleteCharacterResponse : ServerPacket
{
    public CharacterUndeleteResult Result;
    public CharacterUndeleteInfo UndeleteInfo;
    public UndeleteCharacterResponse() : base(ServerOpcodes.UndeleteCharacterResponse) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(UndeleteInfo.ClientToken);
        WorldPacket.WriteUInt32((uint)Result);
        WorldPacket.WritePackedGuid(UndeleteInfo.CharacterGuid);
    }
}