// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

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