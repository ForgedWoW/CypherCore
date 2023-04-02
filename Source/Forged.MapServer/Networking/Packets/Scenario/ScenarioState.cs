// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Achievements;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Scenario;

internal class ScenarioState : ServerPacket
{
    public List<BonusObjectiveData> BonusObjectives = new();
    public List<CriteriaProgressPkt> CriteriaProgress = new();
    public int CurrentStep = -1;
    public uint DifficultyID;
    public List<uint> PickedSteps = new();
    public ObjectGuid PlayerGUID;
    public bool ScenarioComplete = false;
    public int ScenarioID;
    public List<ScenarioSpellUpdate> Spells = new();
    public uint TimerDuration;
    public uint WaveCurrent;
    public uint WaveMax;
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