// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

public class AchievementDeleted : ServerPacket
{
    public uint AchievementID;
    public uint Immunities; // this is just garbage, not used by client
    public AchievementDeleted() : base(ServerOpcodes.AchievementDeleted, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(AchievementID);
        WorldPacket.WriteUInt32(Immunities);
    }
}