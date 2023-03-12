// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum EnchantProcAttributes
{
	WhiteHit = 0x01, // enchant shall only proc off white hits (not abilities)
	Limit60 = 0x02   // enchant effects shall be reduced past lvl 60
}