// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

class AccountCriteriaUpdate : ServerPacket
{
	public CriteriaProgressPkt Progress;
	public AccountCriteriaUpdate() : base(ServerOpcodes.AccountCriteriaUpdate) { }

	public override void Write()
	{
		Progress.Write(_worldPacket);
	}
}