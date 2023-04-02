// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Misc;

public class SetRaidDifficulty : ClientPacket
{
    public int DifficultyID;
    public byte Legacy;
    public SetRaidDifficulty(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        DifficultyID = WorldPacket.ReadInt32();
        Legacy = WorldPacket.ReadUInt8();
    }
}