// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Reputation;

namespace Game.Common.Networking.Packets.Reputation;

public class SetFactionStanding : ServerPacket
{
	public float BonusFromAchievementSystem;
	public List<FactionStandingData> Faction = new();
	public bool ShowVisual;
	public SetFactionStanding() : base(ServerOpcodes.SetFactionStanding, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteFloat(BonusFromAchievementSystem);

		_worldPacket.WriteInt32(Faction.Count);

		foreach (var factionStanding in Faction)
			factionStanding.Write(_worldPacket);

		_worldPacket.WriteBit(ShowVisual);
		_worldPacket.FlushBits();
	}
}
