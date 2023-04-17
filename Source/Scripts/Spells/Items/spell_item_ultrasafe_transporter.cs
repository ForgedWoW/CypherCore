// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 36941 - Ultrasafe Transporter: Toshley's Station
internal class SpellItemUltrasafeTransporter : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        return Caster.IsPlayer;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.TeleportUnits, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        if (!RandomHelper.randChance(50)) // 50% success
            return;

        var caster = Caster;

        uint spellId = 0;

        switch (RandomHelper.URand(0, 6))
        {
            case 0:
                spellId = ItemSpellIds.TRANSPORTER_MALFUNCTION_SMALLER;

                break;
            case 1:
                spellId = ItemSpellIds.TRANSPORTER_MALFUNCTION_BIGGER;

                break;
            case 2:
                spellId = ItemSpellIds.SOUL_SPLIT_EVIL;

                break;
            case 3:
                spellId = ItemSpellIds.SOUL_SPLIT_GOOD;

                break;
            case 4:
                if (caster.AsPlayer.TeamId == TeamIds.Alliance)
                    spellId = ItemSpellIds.TRANSFORM_HORDE;
                else
                    spellId = ItemSpellIds.TRANSFORM_ALLIANCE;

                break;
            case 5:
                spellId = ItemSpellIds.TRANSPORTER_MALFUNCTION_CHICKEN;

                break;
            case 6:
                spellId = ItemSpellIds.EVIL_TWIN;

                break;
        }

        caster.SpellFactory.CastSpell(caster, spellId, true);
    }
}