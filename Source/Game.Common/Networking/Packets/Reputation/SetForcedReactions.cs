// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Reputation;

public class SetForcedReactions : ServerPacket
{
	public List<ForcedReaction> Reactions = new();
	public SetForcedReactions() : base(ServerOpcodes.SetForcedReactions, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Reactions.Count);

		foreach (var reaction in Reactions)
			reaction.Write(_worldPacket);
	}
}
