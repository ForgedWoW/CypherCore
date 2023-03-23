// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Achievements;

public class AchievementEarned : ServerPacket
{
	public ObjectGuid Earner;
	public uint EarnerNativeRealm;
	public uint EarnerVirtualRealm;
	public uint AchievementID;
	public long Time;
	public bool Initial;
	public ObjectGuid Sender;
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
