// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// New Bladestorm - 222634
[SpellScript(222634)]
public class SpellWarrBladestormNew : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodicDummy, 0, AuraType.PeriodicDummy));
    }

    private void HandlePeriodicDummy(AuraEffect unnamedParameter)
    {
        Caster.SpellFactory.CastSpell(Caster, 50622, true); // Bladestorm main hand damage
    }
}