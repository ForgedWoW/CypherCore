// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(207317)]
public class SpellDkEpidemic : SpellScript, IHasSpellEffects, ISpellCheckCast, ISpellOnHit
{
    private readonly List<Unit> _savedTargets = new();
    public List<ISpellEffect> SpellEffects { get; } = new();

    public SpellCastResult CheckCast()
    {
        _savedTargets.Clear();
        Caster.GetEnemiesWithinRangeWithOwnedAura(_savedTargets, SpellInfo.GetMaxRange(), DeathKnightSpells.VIRULENT_PLAGUE);

        if (!_savedTargets.Empty())
            return SpellCastResult.SpellCastOk;

        return SpellCastResult.NoValidTargets;
    }

    public void OnHit()
    {
        PreventHitAura();
        var caster = Caster;

        if (!_savedTargets.Empty())
            foreach (var tar in _savedTargets)
            {
                var aura = tar.GetAura(DeathKnightSpells.VIRULENT_PLAGUE, caster.GUID);

                if (aura != null)
                    Caster.SpellFactory.CastSpell(tar, DeathKnightSpells.EPIDEMIC_DAMAGE, true);
            }
    }
}