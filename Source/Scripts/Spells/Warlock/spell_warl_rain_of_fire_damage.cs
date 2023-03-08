// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.RAIN_OF_FIRE_DAMAGE)] 
    internal class spell_warl_rain_of_fire_damage : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            var caster = GetCaster();
            if (caster.TryGetAura(WarlockSpells.INFERNO_AURA, out var inferno))
            {
                if (RandomHelper.randChance(inferno.GetEffect(0).BaseAmount))
                    caster.ModifyPower(PowerType.SoulShards, 1);
            }
        }
    }
}