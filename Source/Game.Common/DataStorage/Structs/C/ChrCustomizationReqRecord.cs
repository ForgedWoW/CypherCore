// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.C;

public sealed class ChrCustomizationReqRecord
{
	public uint Id;
	public string ReqSource;
	public int Flags;
	public int ClassMask;
	public int AchievementID;
	public int QuestID;
	public int OverrideArchive; // -1: allow any, otherwise must match OverrideArchive cvar
	public uint ItemModifiedAppearanceID;

	public ChrCustomizationReqFlag GetFlags()
	{
		return (ChrCustomizationReqFlag)Flags;
	}
}
