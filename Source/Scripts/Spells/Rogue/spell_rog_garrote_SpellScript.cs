// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(703)]
public class spell_rog_garrote_SpellScript : SpellScript, ISpellOnHit
{
    private bool _stealthed;


    public override bool Load()
    {
        if (Caster.HasAuraType(AuraType.ModStealth))
            _stealthed = true;

        return true;
    }

    public void OnHit()
    {
        var caster = Caster;
        var target = ExplTargetUnit;

        if (_stealthed)
            caster.CastSpell(target, RogueSpells.GARROTE_SILENCE, true);
    }
}