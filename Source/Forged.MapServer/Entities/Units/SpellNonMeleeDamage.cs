// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class SpellNonMeleeDamage
{
    public double Absorb;
    public Unit Attacker;
    public double Blocked;
    public ObjectGuid CastId;
    // Used for help
    public double CleanDamage;

    public double Damage;
    public bool FullBlock;
    public int HitInfo;
    public double OriginalDamage;
    public bool PeriodicLog;
    public long PreHitHealth;
    public double Resist;
    public SpellSchoolMask SchoolMask;
    public SpellInfo Spell;
    public SpellCastVisual SpellVisual;
    public Unit Target;
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