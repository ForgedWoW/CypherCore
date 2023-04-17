// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenSeaforiumBlast : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        // OriginalCaster is always available in Spell.prepare
        return GObjCaster.OwnerGUID.IsPlayer;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(AchievementCredit, 1, SpellEffectName.GameObjectDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void AchievementCredit(int effIndex)
    {
        // but in effect handling OriginalCaster can become null
        var owner = GObjCaster.OwnerUnit;

        if (owner != null)
        {
            var go = HitGObj;

            if (go)
                if (go.Template.type == GameObjectTypes.DestructibleBuilding)
                    owner.SpellFactory.CastSpell(null, GenericSpellIds.PLANT_CHARGES_CREDIT_ACHIEVEMENT, true);
        }
    }
}