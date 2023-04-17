// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.BLASPHEMY)]
public class AuraWarlBlasphemy : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(Apply, 1, AuraType.Dummy, AuraEffectHandleModes.Real));
    }

    private void Apply(AuraEffect aura, AuraEffectHandleModes auraMode)
    {
        if (Caster.TryGetAsPlayer(out var p) && p.TryGetAura(WarlockSpells.AVATAR_OF_DESTRUCTION, out var avatar))
        {
            var time = avatar.GetEffect(0).Amount * Time.IN_MILLISECONDS;
            MaxDuration = (int)time;
            SetDuration(time);
        }
    }
}