// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Units;

public class SpellNonMeleeDamage
{
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

    public double Absorb { get; set; }
    public Unit Attacker { get; set; }
    public double Blocked { get; set; }
    public ObjectGuid CastId { get; set; }

    // Used for help
    public double CleanDamage { get; set; }
    public double Damage { get; set; }
    public bool FullBlock { get; set; }
    public int HitInfo { get; set; }
    public double OriginalDamage { get; set; }
    public bool PeriodicLog { get; set; }
    public long PreHitHealth { get; set; }
    public double Resist { get; set; }
    public SpellSchoolMask SchoolMask { get; set; }
    public SpellInfo Spell { get; set; }
    public SpellCastVisual SpellVisual { get; set; }
    public Unit Target { get; set; }
}