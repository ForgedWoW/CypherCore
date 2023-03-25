// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

struct PetStableInfo
{
	public uint PetSlot;
	public uint PetNumber;
	public uint CreatureID;
	public uint DisplayID;
	public uint ExperienceLevel;
	public PetStableinfo PetFlags;
	public string PetName;
}