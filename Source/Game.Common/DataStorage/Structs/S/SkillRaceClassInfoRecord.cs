// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.S;

public sealed class SkillRaceClassInfoRecord
{
	public uint Id;
	public long RaceMask;
	public ushort SkillID;
	public int ClassMask;
	public SkillRaceClassInfoFlags Flags;
	public sbyte Availability;
	public sbyte MinLevel;
	public ushort SkillTierID;
}
