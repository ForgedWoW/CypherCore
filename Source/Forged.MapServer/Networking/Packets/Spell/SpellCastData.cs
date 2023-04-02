﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellCastData
{
    public SpellAmmo Ammo;
    public ObjectGuid CasterGUID;
    public ObjectGuid CasterUnit;
    public SpellCastFlags CastFlags;
    public SpellCastFlagsEx CastFlagsEx;
    public ObjectGuid CastID;
    public uint CastTime;
    public byte DestLocSpellCastIndex;
    public List<SpellHitStatus> HitStatus = new();
    public List<ObjectGuid> HitTargets = new();
    public CreatureImmunities Immunities;
    public MissileTrajectoryResult MissileTrajectory;
    public List<SpellMissStatus> MissStatus = new();
    public List<ObjectGuid> MissTargets = new();
    public ObjectGuid OriginalCastID;
    public SpellHealPrediction Predict;
    public List<SpellPowerData> RemainingPower = new();
    public RuneData RemainingRunes;
    public int SpellID;
    public SpellTargetData Target = new();
    public List<TargetLocation> TargetPoints = new();
    public SpellCastVisual Visual;
    public void Write(WorldPacket data)
    {
        data.WritePackedGuid(CasterGUID);
        data.WritePackedGuid(CasterUnit);
        data.WritePackedGuid(CastID);
        data.WritePackedGuid(OriginalCastID);
        data.WriteInt32(SpellID);

        Visual.Write(data);

        data.WriteUInt32((uint)CastFlags);
        data.WriteUInt32((uint)CastFlagsEx);
        data.WriteUInt32(CastTime);

        MissileTrajectory.Write(data);

        data.WriteInt32(Ammo.DisplayID);
        data.WriteUInt8(DestLocSpellCastIndex);

        Immunities.Write(data);
        Predict.Write(data);

        data.WriteBits(HitTargets.Count, 16);
        data.WriteBits(MissTargets.Count, 16);
        data.WriteBits(HitStatus.Count, 16);
        data.WriteBits(MissStatus.Count, 16);
        data.WriteBits(RemainingPower.Count, 9);
        data.WriteBit(RemainingRunes != null);
        data.WriteBits(TargetPoints.Count, 16);
        data.FlushBits();

        foreach (var missStatus in MissStatus)
            missStatus.Write(data);

        Target.Write(data);

        foreach (var hitTarget in HitTargets)
            data.WritePackedGuid(hitTarget);

        foreach (var missTarget in MissTargets)
            data.WritePackedGuid(missTarget);

        foreach (var hitStatus in HitStatus)
            hitStatus.Write(data);

        foreach (var power in RemainingPower)
            power.Write(data);

        RemainingRunes?.Write(data);

        foreach (var targetLoc in TargetPoints)
            targetLoc.Write(data);
    }
}