// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(190984)]
public class spell_druid_solar_wrath : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHitTarget(int effIndex)
    {
        var target = HitUnit;

        if (target != null)
            if (Caster.HasAura(Spells.NATURES_BALANCE))
            {
                var sunfireDOT = target.GetAura(Spells.SUNFIRE_DOT, Caster.GUID);

                if (sunfireDOT != null)
                {
                    var duration = sunfireDOT.Duration;
                    var newDuration = duration + 4 * Time.IN_MILLISECONDS;

                    if (newDuration > sunfireDOT.MaxDuration)
                        sunfireDOT.SetMaxDuration(newDuration);

                    sunfireDOT.SetDuration(newDuration);
                }
            }

        if (Caster && RandomHelper.randChance(20) && Caster.HasAura(DruidSpells.ECLIPSE))
            Caster.CastSpell(null, DruidSpells.LUNAR_EMPOWEREMENT, true);
    }


    private struct Spells
    {
        public static readonly uint SOLAR_WRATH = 190984;
        public static readonly uint NATURES_BALANCE = 202430;
        public static readonly uint SUNFIRE_DOT = 164815;
    }
}