// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 187874 - Crash Lightning
[SpellScript(187874)]
internal class SpellShaCrashLightning : SpellScript, ISpellAfterCast, IHasSpellEffects
{
    private int _targetsHit;

    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterCast()
    {
        if (_targetsHit >= 2)
            Caster.SpellFactory.CastSpell(Caster, ShamanSpells.CRASH_LIGHTNING_CLEAVE, true);

        var gatheringStorms = Caster.GetAuraEffect(ShamanSpells.GATHERING_STORMS, 0);

        if (gatheringStorms != null)
        {
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)(gatheringStorms.Amount * _targetsHit));
            Caster.SpellFactory.CastSpell(Caster, ShamanSpells.GATHERING_STORMS_BUFF, args);
        }
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitConeCasterToDestEnemy));
    }

    private void CountTargets(List<WorldObject> targets)
    {
        _targetsHit = targets.Count;
    }
}