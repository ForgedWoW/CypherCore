// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Spells;

internal class SpellDiminishInfo
{
    public int DiminishDurationLimit { get; set; }
    public DiminishingGroup DiminishGroup { get; set; } = DiminishingGroup.None;
    public DiminishingLevels DiminishMaxLevel { get; set; } = DiminishingLevels.Immune;
    public DiminishingReturnsType DiminishReturnType { get; set; } = DiminishingReturnsType.None;
}