// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

/// Item - Death Knight T17 Frost 4P Driver (Periodic) - 170205
[SpellScript(170205)]
public class SpellDkItemT17Frost4PDriverPeriodic : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicTriggerSpell));
    }

    private void OnTick(AuraEffect unnamedParameter)
    {
        var lCaster = Caster;

        if (lCaster == null)
            return;

        var lTarget = lCaster.Victim;

        if (lTarget == null)
            return;

        var lPlayer = lCaster.AsPlayer;

        if (lPlayer != null)
        {
            var lAura = lPlayer.GetAura(ESpells.FROZEN_RUNEBLADE_STACKS);

            if (lAura != null)
            {
                var lMainHand = lPlayer.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

                if (lMainHand != null)
                    lPlayer.SpellFactory.CastSpell(lTarget, ESpells.FROZEN_RUNEBLADE_MAIN_HAND, true);

                var lOffHand = lPlayer.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

                if (lOffHand != null)
                    lPlayer.SpellFactory.CastSpell(lTarget, ESpells.FROZEN_RUNEBLADE_OFF_HAND, true);

                lAura.DropCharge();
            }
        }
    }

    private struct ESpells
    {
        public const uint FROZEN_RUNEBLADE_MAIN_HAND = 165569;
        public const uint FROZEN_RUNEBLADE_OFF_HAND = 178808;
        public const uint FROZEN_RUNEBLADE_STACKS = 170202;
    }
}