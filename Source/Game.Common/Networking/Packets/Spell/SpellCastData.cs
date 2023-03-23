// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Spell;

namespace Game.Common.Networking.Packets.Spell;

public class SpellCastData
{
	public ObjectGuid CasterGUID;
	public ObjectGuid CasterUnit;
	public ObjectGuid CastID;
	public ObjectGuid OriginalCastID;
	public int SpellID;
	public SpellCastVisual Visual;
	public SpellCastFlags CastFlags;
	public SpellCastFlagsEx CastFlagsEx;
	public uint CastTime;
	public List<ObjectGuid> HitTargets = new();
	public List<ObjectGuid> MissTargets = new();
	public List<SpellHitStatus> HitStatus = new();
	public List<SpellMissStatus> MissStatus = new();
	public SpellTargetData Target = new();
	public List<SpellPowerData> RemainingPower = new();
	public RuneData RemainingRunes;
	public MissileTrajectoryResult MissileTrajectory;
	public SpellAmmo Ammo;
	public byte DestLocSpellCastIndex;
	public List<TargetLocation> TargetPoints = new();
	public CreatureImmunities Immunities;
	public SpellHealPrediction Predict;

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

		if (RemainingRunes != null)
			RemainingRunes.Write(data);

		foreach (var targetLoc in TargetPoints)
			targetLoc.Write(data);
	}
}
