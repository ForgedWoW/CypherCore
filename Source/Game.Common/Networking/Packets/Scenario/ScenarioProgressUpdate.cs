// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Achievements;

namespace Game.Common.Networking.Packets.Scenario;

public class ScenarioProgressUpdate : ServerPacket
{
	public CriteriaProgressPkt CriteriaProgress;
	public ScenarioProgressUpdate() : base(ServerOpcodes.ScenarioProgressUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		CriteriaProgress.Write(_worldPacket);
	}
}
