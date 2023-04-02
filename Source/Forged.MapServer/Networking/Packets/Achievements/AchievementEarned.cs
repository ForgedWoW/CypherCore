// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

public class AchievementEarned : ServerPacket
{
    public uint AchievementID;
    public ObjectGuid Earner;
    public uint EarnerNativeRealm;
    public uint EarnerVirtualRealm;
    public bool Initial;
    public ObjectGuid Sender;
    public long Time;
    public AchievementEarned() : base(ServerOpcodes.AchievementEarned, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Sender);
        WorldPacket.WritePackedGuid(Earner);
        WorldPacket.WriteUInt32(AchievementID);
        WorldPacket.WritePackedTime(Time);
        WorldPacket.WriteUInt32(EarnerNativeRealm);
        WorldPacket.WriteUInt32(EarnerVirtualRealm);
        WorldPacket.WriteBit(Initial);
        WorldPacket.FlushBits();
    }
}