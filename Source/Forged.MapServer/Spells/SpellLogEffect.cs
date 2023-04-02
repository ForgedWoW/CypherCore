// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Networking.Packets.CombatLog;

namespace Forged.MapServer.Spells;

public class SpellLogEffect
{
    public List<SpellLogEffectDurabilityDamageParams> DurabilityDamageTargets = new();
    public int Effect;

    public List<SpellLogEffectExtraAttacksParams> ExtraAttacksTargets = new();
    public List<SpellLogEffectFeedPetParams> FeedPetTargets = new();
    public List<SpellLogEffectGenericVictimParams> GenericVictimTargets = new();
    public List<SpellLogEffectPowerDrainParams> PowerDrainTargets = new();
    public List<SpellLogEffectTradeSkillItemParams> TradeSkillTargets = new();
}