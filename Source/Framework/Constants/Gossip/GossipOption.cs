// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GossipOption
{
	None = 0,                   //Unit_Npc_Flag_None                (0)
	Gossip = 1,                 //Unit_Npc_Flag_Gossip              (1)
	Questgiver = 2,             //Unit_Npc_Flag_Questgiver          (2)
	Vendor = 3,                 //Unit_Npc_Flag_Vendor              (128)
	Taxivendor = 4,             //Unit_Npc_Flag_Taxivendor          (8192)
	Trainer = 5,                //Unit_Npc_Flag_Trainer             (16)
	Spirithealer = 6,           //Unit_Npc_Flag_Spirithealer        (16384)
	Spiritguide = 7,            //Unit_Npc_Flag_Spiritguide         (32768)
	Innkeeper = 8,              //Unit_Npc_Flag_Innkeeper           (65536)
	Banker = 9,                 //Unit_Npc_Flag_Banker              (131072)
	Petitioner = 10,            //Unit_Npc_Flag_Petitioner          (262144)
	Tabarddesigner = 11,        //Unit_Npc_Flag_Tabarddesigner      (524288)
	Battlefield = 12,           //Unit_Npc_Flag_Battlefieldperson   (1048576)
	Auctioneer = 13,            //Unit_Npc_Flag_Auctioneer          (2097152)
	Stablepet = 14,             //Unit_Npc_Flag_Stable              (4194304)
	Armorer = 15,               //Unit_Npc_Flag_Armorer             (4096)
	Unlearntalents = 16,        //Unit_Npc_Flag_Trainer             (16) (Bonus Option For Trainer)
	Unlearnpettalents_Old = 17, // deprecated
	Learndualspec = 18,         //Unit_Npc_Flag_Trainer             (16) (Bonus Option For Trainer)
	Outdoorpvp = 19,            //Added By Code (Option For Outdoor Pvp Creatures)
	Transmogrifier = 20,        //UNIT_NPC_FLAG_TRANSMOGRIFIER
	Mailbox = 21,               //UNIT_NPC_FLAG_MAILBOX
	Max
}