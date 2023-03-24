// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Scenario;

public class ScenarioCompleted : ServerPacket
{
	public uint ScenarioID;

	public ScenarioCompleted(uint scenarioId) : base(ServerOpcodes.ScenarioCompleted, ConnectionType.Instance)
	{
		ScenarioID = scenarioId;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(ScenarioID);
	}
}
