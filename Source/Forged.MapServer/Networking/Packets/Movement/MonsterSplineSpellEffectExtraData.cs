// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Movement;

public struct MonsterSplineSpellEffectExtraData
{
	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(TargetGuid);
		data.WriteUInt32(SpellVisualID);
		data.WriteUInt32(ProgressCurveID);
		data.WriteUInt32(ParabolicCurveID);
		data.WriteFloat(JumpGravity);
	}

	public ObjectGuid TargetGuid;
	public uint SpellVisualID;
	public uint ProgressCurveID;
	public uint ParabolicCurveID;
	public float JumpGravity;
}