// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(118009)]
public class spell_dk_desecrated_ground : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 1, AuraType.PeriodicDummy));
    }

    private void OnTick(AuraEffect UnnamedParameter)
    {
        if (Caster)
        {
            var dynObj = Caster.GetDynObject(DeathKnightSpells.DESECRATED_GROUND);

            if (dynObj != null)
                if (Caster.GetDistance(dynObj) <= 8.0f)
                    Caster.CastSpell(Caster, DeathKnightSpells.DESECRATED_GROUND_IMMUNE, true);
        }
    }
}