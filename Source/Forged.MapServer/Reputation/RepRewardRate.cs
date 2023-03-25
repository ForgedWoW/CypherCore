// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Reputation;

public class RepRewardRate
{
	public float QuestRate; // We allow rate = 0.0 in database. For this case, it means that
	public float QuestDailyRate;
	public float QuestWeeklyRate;
	public float QuestMonthlyRate;
	public float QuestRepeatableRate;
	public float CreatureRate; // no reputation are given at all for this faction/rate type.
	public float SpellRate;
}