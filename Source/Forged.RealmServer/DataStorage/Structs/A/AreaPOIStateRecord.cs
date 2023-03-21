// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public class AreaPOIStateRecord
{
	public uint ID;
	public LocalizedString Description;
	public byte WorldStateValue;
	public byte IconEnumValue;
	public uint UiTextureAtlasMemberID;
	public uint AreaPoiID;
}