// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BagFamilyMask
{
	None = 0x00,
	Arrows = 0x01,
	Bullets = 0x02,
	SoulShards = 0x04,
	LeatherworkingSupp = 0x08,
	InscriptionSupp = 0x10,
	Herbs = 0x20,
	EnchantingSupp = 0x40,
	EngineeringSupp = 0x80,
	Keys = 0x100,
	Gems = 0x200,
	MiningSupp = 0x400,
	SoulboundEquipment = 0x800,
	VanityPets = 0x1000,
	CurrencyTokens = 0x2000,
	QuestItems = 0x4000,
	FishingSupp = 0x8000,
	CookingSupp = 0x10000
}