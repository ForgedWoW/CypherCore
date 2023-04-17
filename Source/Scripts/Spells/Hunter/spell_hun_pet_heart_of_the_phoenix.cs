// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[Script]
internal class SpellHunPetHeartOfThePhoenix : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        if (!Caster.IsPet)
            return false;

        return true;
    }


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;
        var owner = caster.OwnerUnit;

        if (owner)
            if (!caster.HasAura(HunterSpells.PetHeartOfThePhoenixDebuff))
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.BasePoint0, 100);
                owner.SpellFactory.CastSpell(caster, HunterSpells.PetHeartOfThePhoenixTriggered, args);
                caster.SpellFactory.CastSpell(caster, HunterSpells.PetHeartOfThePhoenixDebuff, true);
            }
    }
}