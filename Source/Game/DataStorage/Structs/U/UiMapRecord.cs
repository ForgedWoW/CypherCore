// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class UiMapRecord
{
	public LocalizedString Name;
	public uint Id;
	public int ParentUiMapID;
	public int Flags;
	public uint System;
	public UiMapType Type;
	public int BountySetID;
	public uint BountyDisplayLocation;
	public int VisibilityPlayerConditionID;
	public sbyte HelpTextPosition;
	public int BkgAtlasID;
	public int AlternateUiMapGroup;
	public int ContentTuningID;

	public UiMapFlag GetFlags()
	{
		return (UiMapFlag)Flags;
	}
}