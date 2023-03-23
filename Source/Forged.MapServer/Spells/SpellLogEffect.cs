// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Common.Networking.Packets.CombatLog;

namespace Game.Spells;

public class SpellLogEffect
{
	public int Effect;

	public List<SpellLogEffectPowerDrainParams> PowerDrainTargets = new();
	public List<SpellLogEffectExtraAttacksParams> ExtraAttacksTargets = new();
	public List<SpellLogEffectDurabilityDamageParams> DurabilityDamageTargets = new();
	public List<SpellLogEffectGenericVictimParams> GenericVictimTargets = new();
	public List<SpellLogEffectTradeSkillItemParams> TradeSkillTargets = new();
	public List<SpellLogEffectFeedPetParams> FeedPetTargets = new();
}