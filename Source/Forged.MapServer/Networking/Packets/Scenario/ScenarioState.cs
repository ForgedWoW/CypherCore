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
        WorldPacket.WriteInt32(ScenarioID);
        WorldPacket.WriteInt32(CurrentStep);
        WorldPacket.WriteUInt32(DifficultyID);
        WorldPacket.WriteUInt32(WaveCurrent);
        WorldPacket.WriteUInt32(WaveMax);
        WorldPacket.WriteUInt32(TimerDuration);
        WorldPacket.WriteInt32(CriteriaProgress.Count);
        WorldPacket.WriteInt32(BonusObjectives.Count);
        WorldPacket.WriteInt32(PickedSteps.Count);
        WorldPacket.WriteInt32(Spells.Count);
        WorldPacket.WritePackedGuid(PlayerGUID);

        for (var i = 0; i < PickedSteps.Count; ++i)
            WorldPacket.WriteUInt32(PickedSteps[i]);

        WorldPacket.WriteBit(ScenarioComplete);
        WorldPacket.FlushBits();

        foreach (var progress in CriteriaProgress)
            progress.Write(WorldPacket);

        foreach (var bonusObjective in BonusObjectives)
            bonusObjective.Write(WorldPacket);

        foreach (var spell in Spells)
            spell.Write(WorldPacket);
    }
}