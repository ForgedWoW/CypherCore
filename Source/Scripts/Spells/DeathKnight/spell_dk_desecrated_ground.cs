// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(118009)]
public class SpellDkDesecratedGround : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 1, AuraType.PeriodicDummy));
    }

    private void OnTick(AuraEffect unnamedParameter)
    {
        if (Caster)
        {
            var dynObj = Caster.GetDynObject(DeathKnightSpells.DESECRATED_GROUND);

            if (dynObj != null)
                if (Caster.GetDistance(dynObj) <= 8.0f)
                    Caster.SpellFactory.CastSpell(Caster, DeathKnightSpells.DESECRATED_GROUND_IMMUNE, true);
        }
    }
}