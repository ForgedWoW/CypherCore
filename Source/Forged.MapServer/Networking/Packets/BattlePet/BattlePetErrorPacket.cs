// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattlePet;

internal class BattlePetErrorPacket : ServerPacket
{
    public uint CreatureID;
    public BattlePetError Result;
    public BattlePetErrorPacket() : base(ServerOpcodes.BattlePetError) { }

    public override void Write()
    {
        WorldPacket.WriteBits(Result, 4);
        WorldPacket.WriteUInt32(CreatureID);
    }
}