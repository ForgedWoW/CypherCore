// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class RandomRoll : ServerPacket
{
    public int Max;
    public int Min;
    public int Result;
    public ObjectGuid Roller;
    public ObjectGuid RollerWowAccount;
    public RandomRoll() : base(ServerOpcodes.RandomRoll) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Roller);
        WorldPacket.WritePackedGuid(RollerWowAccount);
        WorldPacket.WriteInt32(Min);
        WorldPacket.WriteInt32(Max);
        WorldPacket.WriteInt32(Result);
    }
}