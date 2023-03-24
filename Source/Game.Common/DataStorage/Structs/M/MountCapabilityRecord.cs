// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Common.DataStorage.Structs.M;

public sealed class MountCapabilityRecord
{
	public uint Id;
	public MountCapabilityFlags Flags;
	public ushort ReqRidingSkill;
	public ushort ReqAreaID;
	public uint ReqSpellAuraID;
	public uint ReqSpellKnownID;
	public uint ModSpellAuraID;
	public short ReqMapID;
	public int PlayerConditionID;
	public int FlightCapabilityID;
}
