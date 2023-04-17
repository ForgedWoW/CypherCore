// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script("spell_gen_sunreaver_disguise")]
[Script("spell_gen_silver_covenant_disguise")]
internal class SpellGenDalaranDisguise : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var player = HitPlayer;

        if (player)
        {
            var gender = player.NativeGender;

            var spellId = SpellInfo.Id;

            switch (spellId)
            {
                case GenericSpellIds.SUNREAVER_TRIGGER:
                    spellId = gender == Gender.Female ? GenericSpellIds.SUNREAVER_FEMALE : GenericSpellIds.SUNREAVER_MALE;

                    break;
                case GenericSpellIds.SILVER_COVENANT_TRIGGER:
                    spellId = gender == Gender.Female ? GenericSpellIds.SILVER_COVENANT_FEMALE : GenericSpellIds.SILVER_COVENANT_MALE;

                    break;
            }

            Caster.SpellFactory.CastSpell(player, spellId, true);
        }
    }
}