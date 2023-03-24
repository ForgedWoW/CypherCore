// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellEmpowerStageRecord
{
	public uint Id;
	public byte Stage;
	public uint DurationMs;
	public uint SpellEmpowerID;
}
