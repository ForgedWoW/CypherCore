// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Scenario;

internal class ScenarioCompleted : ServerPacket
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