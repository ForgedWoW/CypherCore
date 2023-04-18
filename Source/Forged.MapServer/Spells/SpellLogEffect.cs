// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.CombatLog;

namespace Forged.MapServer.Spells;

public class SpellLogEffect
{
    public List<SpellLogEffectDurabilityDamageParams> DurabilityDamageTargets { get; set; } = new();
    public int Effect { get; set; }

    public List<SpellLogEffectExtraAttacksParams> ExtraAttacksTargets { get; set; } = new();
    public List<SpellLogEffectFeedPetParams> FeedPetTargets { get; set; } = new();
    public List<SpellLogEffectGenericVictimParams> GenericVictimTargets { get; set; } = new();
    public List<SpellLogEffectPowerDrainParams> PowerDrainTargets { get; set; } = new();
    public List<SpellLogEffectTradeSkillItemParams> TradeSkillTargets { get; set; } = new();
}