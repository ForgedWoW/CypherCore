// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GroupUpdatePetFlags
{
	None = 0x00000000,    // nothing
	GUID = 0x00000001,    // ObjectGuid (pet guid)
	Name = 0x00000002,    // cstring (name, NULL terminated string)
	ModelId = 0x00000004, // public ushort (model id)
	CurHp = 0x00000008,   // uint32 (HP)
	MaxHp = 0x00000010,   // uint32 (max HP)
	Auras = 0x00000020,   // [see GROUP_UPDATE_FLAG_AURAS]

	Full = GUID | Name | ModelId | CurHp | MaxHp | Auras // all pet flags
}