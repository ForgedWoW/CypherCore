// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities;

public class SpellNonMeleeDamage
{
	public Unit Target;
	public Unit Attacker;
	public ObjectGuid CastId;
	public SpellInfo Spell;
	public SpellCastVisual SpellVisual;
	public double Damage;
	public double OriginalDamage;
	public SpellSchoolMask SchoolMask;
	public double Absorb;
	public double Resist;
	public bool PeriodicLog;
	public double Blocked;

	public int HitInfo;

	// Used for help
	public double CleanDamage;
	public bool FullBlock;
	public long PreHitHealth;

	public SpellNonMeleeDamage(Unit attacker, Unit target, SpellInfo spellInfo, SpellCastVisual spellVisual, SpellSchoolMask schoolMask, ObjectGuid castId = default)
	{
		Target = target;
		Attacker = attacker;
		Spell = spellInfo;
		SpellVisual = spellVisual;
		SchoolMask = schoolMask;
		CastId = castId;
		PreHitHealth = (uint)target.GetHealth();

		if (attacker == target)
			HitInfo |= (int)SpellHitType.VictimIsAttacker;
	}
}