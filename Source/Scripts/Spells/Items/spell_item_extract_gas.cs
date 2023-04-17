// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 30427 - Extract Gas (23821: Zapthrottle Mote Extractor)
internal class SpellItemExtractGas : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();

        // move loot to player inventory and despawn Target
        if (Caster != null &&
            Caster.IsTypeId(TypeId.Player) &&
            Target.IsTypeId(TypeId.Unit) &&
            Target.AsCreature.Template.CreatureType == CreatureType.GasCloud)
        {
            var player = Caster.AsPlayer;
            var creature = Target.AsCreature;

            // missing lootid has been reported on startup - just return
            if (creature.Template.SkinLootId == 0)
                return;

            player.AutoStoreLoot(creature.Template.SkinLootId, LootStorage.Skinning, ItemContext.None, true);
            creature.DespawnOrUnsummon();
        }
    }
}