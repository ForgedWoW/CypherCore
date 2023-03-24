// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Achievements;

public class RespondInspectAchievements : ServerPacket
{
	public ObjectGuid Player;
	public AllAchievements Data = new();
	public RespondInspectAchievements() : base(ServerOpcodes.RespondInspectAchievements, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Player);
		Data.Write(_worldPacket);
	}
}
