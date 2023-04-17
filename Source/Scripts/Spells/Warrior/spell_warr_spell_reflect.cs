// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 23920 Spell Reflect
[SpellScript(23920)]
public class SpellWarrSpellReflect : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ReflectSpells, AuraEffectHandleModes.RealOrReapplyMask));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ReflectSpells, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectRemove));
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null || caster.TypeId != TypeId.Player)
            return;

        var item = caster.AsPlayer.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

        if (item != null && item.Template.InventoryType == InventoryType.Shield)
            caster.SpellFactory.CastSpell(caster, 146120, true);
        else if (caster.Faction == 1732) // Alliance
            caster.SpellFactory.CastSpell(caster, 147923, true);
        else // Horde
            caster.SpellFactory.CastSpell(caster, 146122, true);
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null || caster.TypeId != TypeId.Player)
            return;

        // Visuals
        caster.RemoveAura(146120);
        caster.RemoveAura(147923);
        caster.RemoveAura(146122);
    }
}