// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum DiminishingReturnsType
{
	None = 0,   // this spell is not diminished, but may have its duration limited
	Player = 1, // this spell is diminished only when applied on players
	All = 2     // this spell is diminished in every case
}