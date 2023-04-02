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
        _worldPacket.WritePackedGuid(Sender);
        _worldPacket.WritePackedGuid(Earner);
        _worldPacket.WriteUInt32(AchievementID);
        _worldPacket.WritePackedTime(Time);
        _worldPacket.WriteUInt32(EarnerNativeRealm);
        _worldPacket.WriteUInt32(EarnerVirtualRealm);
        _worldPacket.WriteBit(Initial);
        _worldPacket.FlushBits();
    }
}