// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum NPCFlags : uint
{
	None = 0x00,
	Gossip = 0x01,     // 100%
	QuestGiver = 0x02, // 100%
	Unk1 = 0x04,
	Unk2 = 0x08,
	Trainer = 0x10,                  // 100%
	TrainerClass = 0x20,             // 100%
	TrainerProfession = 0x40,        // 100%
	Vendor = 0x80,                   // 100%
	VendorAmmo = 0x100,              // 100%, General Goods Vendor
	VendorFood = 0x200,              // 100%
	VendorPoison = 0x400,            // Guessed
	VendorReagent = 0x800,           // 100%
	Repair = 0x1000,                 // 100%
	FlightMaster = 0x2000,           // 100%
	SpiritHealer = 0x4000,           // Guessed
	SpiritGuide = 0x8000,            // Guessed
	Innkeeper = 0x10000,             // 100%
	Banker = 0x20000,                // 100%
	Petitioner = 0x40000,            // 100% 0xc0000 = Guild Petitions, 0x40000 = Arena Team Petitions
	TabardDesigner = 0x80000,        // 100%
	BattleMaster = 0x100000,         // 100%
	Auctioneer = 0x200000,           // 100%
	StableMaster = 0x400000,         // 100%
	GuildBanker = 0x800000,          //
	SpellClick = 0x1000000,          //
	PlayerVehicle = 0x2000000,       // Players With Mounts That Have Vehicle Data Should Have It Set
	Mailbox = 0x4000000,             // Mailbox
	ArtifactPowerRespec = 0x8000000, // Artifact Powers Reset
	Transmogrifier = 0x10000000,     // Transmogrification
	VaultKeeper = 0x20000000,        // Void Storage
	WildBattlePet = 0x40000000,      // Pet That Player Can Fight (Battle Pet)
	BlackMarket = 0x80000000,        // Black Market
}