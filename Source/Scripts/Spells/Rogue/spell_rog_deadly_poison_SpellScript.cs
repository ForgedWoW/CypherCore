// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Scripts.Spells.Rogue;

[Script] // 2818 - Deadly Poison
internal class SpellRogDeadlyPoisonSpellScript : SpellScript, ISpellBeforeHit, ISpellAfterHit
{
    private byte _stackAmount = 0;

    public void AfterHit()
    {
        if (_stackAmount < 5)
            return;

        var player = Caster.AsPlayer;
        var target = HitUnit;

        if (target != null)
        {
            var item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

            if (item == CastItem)
                item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

            if (!item)
                return;

            // Item combat enchantments
            for (byte slot = 0; slot < (int)EnchantmentSlot.Max; ++slot)
            {
                var enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item.GetEnchantmentId((EnchantmentSlot)slot));

                if (enchant == null)
                    continue;

                for (byte s = 0; s < 3; ++s)
                {
                    if (enchant.Effect[s] != ItemEnchantmentType.CombatSpell)
                        continue;

                    var spellInfo = Global.SpellMgr.GetSpellInfo(enchant.EffectArg[s], Difficulty.None);

                    if (spellInfo == null)
                    {
                        Log.Logger.Error($"Player::CastItemCombatSpell Enchant {enchant.Id}, player (Name: {player.GetName()}, {player.GUID}) cast unknown spell {enchant.EffectArg[s]}");

                        continue;
                    }

                    // Proc only rogue poisons
                    if (spellInfo.SpellFamilyName != SpellFamilyNames.Rogue ||
                        spellInfo.Dispel != DispelType.Poison)
                        continue;

                    // Do not reproc deadly
                    if (spellInfo.SpellFamilyFlags & new FlagArray128(0x10000))
                        continue;

                    if (spellInfo.IsPositive)
                        player.SpellFactory.CastSpell(player, enchant.EffectArg[s], item);
                    else
                        player.SpellFactory.CastSpell(target, enchant.EffectArg[s], item);
                }
            }
        }
    }

    public override bool Load()
    {
        // at this point CastItem must already be initialized
        return Caster.IsPlayer && CastItem;
    }

    public void BeforeHit(SpellMissInfo missInfo)
    {
        if (missInfo != SpellMissInfo.None)
            return;

        var target = HitUnit;

        if (target != null)
        {
            // Deadly Poison
            var aurEff = target.GetAuraEffect(AuraType.PeriodicDummy, SpellFamilyNames.Rogue, new FlagArray128(0x10000, 0x80000, 0), Caster.GUID);

            if (aurEff != null)
                _stackAmount = aurEff.Base.StackAmount;
        }
    }
}