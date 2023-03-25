// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

struct PetRenameData
{
	public ObjectGuid PetGUID;
	public int PetNumber;
	public string NewName;
	public bool HasDeclinedNames;
	public DeclinedName DeclinedNames;
}