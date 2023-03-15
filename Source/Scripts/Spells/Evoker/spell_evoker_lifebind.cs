// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_FIRE_BREATH, EvokerSpells.RED_FIRE_BREATH_2)]
internal class spell_evoker_lifebind : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        if (Caster.HasSpell(EvokerSpells.LIFEBIND) && Caster != ExplTargetUnit)
        {
            var aura = Caster.AddAura(EvokerSpells.LIFEBIND_AURA);
            aura.ForEachAuraScript<IAuraScriptValues>(a => a.ScriptValues["target"] = ExplTargetUnit);
        }
    }
}