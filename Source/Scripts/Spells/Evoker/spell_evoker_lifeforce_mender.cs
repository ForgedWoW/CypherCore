// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_LIVING_FLAME_HEAL,
                EvokerSpells.RED_LIVING_FLAME_DAMAGE,
                EvokerSpells.RED_FIRE_BREATH_CHARGED)]
internal class spell_evoker_lifeforce_mender : SpellScript, ISpellOnCast
{
    public void OnCast()
    {
        if (!Caster.TryGetAura(EvokerSpells.LIFEFORCE_MENDER, out var aura))
            return;

        aura.GetEffect(1).SetAmount(Caster.MaxHealth * (aura.GetEffect(0).Amount * 0.01));
    }
}