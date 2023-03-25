// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class SpellCooldownsRecord
{
	public uint Id;
	public byte DifficultyID;
	public uint CategoryRecoveryTime;
	public uint RecoveryTime;
	public uint StartRecoveryTime;
	public uint AuraSpellID;
	public uint SpellID;
}