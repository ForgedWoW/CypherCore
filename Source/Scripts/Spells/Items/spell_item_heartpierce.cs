// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script("spell_item_heartpierce", ItemSpellIds.INVIGORATION_ENERGY, ItemSpellIds.INVIGORATION_MANA, ItemSpellIds.INVIGORATION_RAGE, ItemSpellIds.INVIGORATION_RP)]
[Script("spell_item_heartpierce_hero", ItemSpellIds.INVIGORATION_ENERGY_HERO, ItemSpellIds.INVIGORATION_MANA_HERO, ItemSpellIds.INVIGORATION_RAGE_HERO, ItemSpellIds.INVIGORATION_RP_HERO)]
internal class SpellItemHeartpierce : AuraScript, IHasAuraEffects
{
    private readonly uint _energySpellId;
    private readonly uint _manaSpellId;
    private readonly uint _rageSpellId;
    private readonly uint _rpSpellId;

    public SpellItemHeartpierce(uint energySpellId, uint manaSpellId, uint rageSpellId, uint rpSpellId)
    {
        _energySpellId = energySpellId;
        _manaSpellId = manaSpellId;
        _rageSpellId = rageSpellId;
        _rpSpellId = rpSpellId;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var caster = eventInfo.Actor;

        uint spellId;

        switch (caster.DisplayPowerType)
        {
            case PowerType.Mana:
                spellId = _manaSpellId;

                break;
            case PowerType.Energy:
                spellId = _energySpellId;

                break;
            case PowerType.Rage:
                spellId = _rageSpellId;

                break;
            // Death Knights can't use daggers, but oh well
            case PowerType.RunicPower:
                spellId = _rpSpellId;

                break;
            default:
                return;
        }

        caster.SpellFactory.CastSpell((Unit)null, spellId, new CastSpellExtraArgs(aurEff));
    }
}