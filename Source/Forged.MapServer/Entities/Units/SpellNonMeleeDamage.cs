// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

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

		if (target != null)
			PreHitHealth = (uint)target.Health;

		if (attacker == target)
			HitInfo |= (int)SpellHitType.VictimIsAttacker;
	}
}