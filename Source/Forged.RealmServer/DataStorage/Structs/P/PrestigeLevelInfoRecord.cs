// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class PrestigeLevelInfoRecord
{
	public uint Id;
	public string Name;
	public int PrestigeLevel;
	public int BadgeTextureFileDataID;
	public PrestigeLevelInfoFlags Flags;
	public int AwardedAchievementID;

	public bool IsDisabled()
	{
		return (Flags & PrestigeLevelInfoFlags.Disabled) != 0;
	}
}