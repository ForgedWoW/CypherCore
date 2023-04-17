// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenBonked : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var target = HitPlayer;

        if (target)
        {
            var aura = GetHitAura();

            if (!(aura != null && aura.StackAmount == 3))
                return;

            target.SpellFactory.CastSpell(target, GenericSpellIds.FORM_SWORD_DEFEAT, true);
            target.RemoveAura(GenericSpellIds.BONKED);

            aura = target.GetAura(GenericSpellIds.ONGUARD);

            if (aura != null)
            {
                var item = target.GetItemByGuid(aura.CastItemGuid);

                if (item)
                    target.DestroyItemCount(item.Entry, 1, true);
            }
        }
    }
}