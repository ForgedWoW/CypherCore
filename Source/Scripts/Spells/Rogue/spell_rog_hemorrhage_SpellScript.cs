// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(16511)]
public class SpellRogHemorrhageSpellScript : SpellScript, ISpellOnHit, ISpellBeforeHit, ISpellAfterHit
{
    private bool _bleeding;

    public void AfterHit()
    {
        var caster = Caster;
        var cp = caster.GetPower(PowerType.ComboPoints);

        if (cp > 0)
            caster.SetPower(PowerType.ComboPoints, cp - 1);
    }

    public void BeforeHit(SpellMissInfo unnamedParameter)
    {
        var target = HitUnit;

        if (target != null)
            _bleeding = target.HasAuraState(AuraStateType.Bleed);
    }

    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (HitUnit)
                if (player.HasAura(RogueSpells.GLYPH_OF_HEMORRHAGE))
                    if (!_bleeding)
                    {
                        PreventHitAura();

                        return;
                    }
    }
}