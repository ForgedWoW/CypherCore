// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Achievements;
using Game.Common.Networking.Packets.Scenario;

namespace Game.Common.Networking.Packets.Scenario;

public class ScenarioState : ServerPacket
{
	public int ScenarioID;
	public int CurrentStep = -1;
	public uint DifficultyID;
	public uint WaveCurrent;
	public uint WaveMax;
	public uint TimerDuration;
	public List<CriteriaProgressPkt> CriteriaProgress = new();
	public List<BonusObjectiveData> BonusObjectives = new();
	public List<uint> PickedSteps = new();
	public List<ScenarioSpellUpdate> Spells = new();
	public ObjectGuid PlayerGUID;
	public bool ScenarioComplete = false;
	public ScenarioState() : base(ServerOpcodes.ScenarioState, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(ScenarioID);
		_worldPacket.WriteInt32(CurrentStep);
		_worldPacket.WriteUInt32(DifficultyID);
		_worldPacket.WriteUInt32(WaveCurrent);
		_worldPacket.WriteUInt32(WaveMax);
		_worldPacket.WriteUInt32(TimerDuration);
		_worldPacket.WriteInt32(CriteriaProgress.Count);
		_worldPacket.WriteInt32(BonusObjectives.Count);
		_worldPacket.WriteInt32(PickedSteps.Count);
		_worldPacket.WriteInt32(Spells.Count);
		_worldPacket.WritePackedGuid(PlayerGUID);

		for (var i = 0; i < PickedSteps.Count; ++i)
			_worldPacket.WriteUInt32(PickedSteps[i]);

		_worldPacket.WriteBit(ScenarioComplete);
		_worldPacket.FlushBits();

		foreach (var progress in CriteriaProgress)
			progress.Write(_worldPacket);

		foreach (var bonusObjective in BonusObjectives)
			bonusObjective.Write(_worldPacket);

		foreach (var spell in Spells)
			spell.Write(_worldPacket);
	}
}
