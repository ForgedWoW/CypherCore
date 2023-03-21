// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class TraitNodeRecord
{
	public uint Id;
	public int TraitTreeID;
	public int PosX;
	public int PosY;
	public sbyte Type;
	public int Flags;

	public TraitNodeType GetNodeType()
	{
		return (TraitNodeType)Type;
	}
}