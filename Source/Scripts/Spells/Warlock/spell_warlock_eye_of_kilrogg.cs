// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 126 - Eye of Kilrogg
[SpellScript(126)]
public class spell_warlock_eye_of_kilrogg : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.ModInvisibilityDetect, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }


    private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
    {
        var caster = Caster;

        if (caster == null || !caster.AsPlayer)
            return;

        if (caster.AsPlayer.CurrentPet)
            caster.Events.AddEventAtOffset(() => { caster.AsPlayer.PetSpellInitialize(); }, TimeSpan.FromMilliseconds(250));
    }
}