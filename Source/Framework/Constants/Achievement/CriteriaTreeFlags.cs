// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CriteriaTreeFlags
{
	ProgressBar = 0x0001,              // Progress Bar
	DoNotDisplay = 0x0002,             // Do Not Display
	IsDate = 0x0004,                   // Is a Date
	IsMoney = 0x0008,                  // Is Money
	ToastOnComplete = 0x0010,          // Toast on Complete
	UseObjectsDescription = 0x0020,    // Use Object's Description
	ShowFactionSpecificChild = 0x0040, // Show faction specific child
	DisplayAllChildren = 0x0080,       // Display all children
	AwardBonusRep = 0x0100,            // Award Bonus Rep (Hack!!)
	AllianceOnly = 0x0200,             // Treat this criteria or block as Alliance
	HordeOnly = 0x0400,                // Treat this criteria or block as Horde
	DisplayAsFraction = 0x0800,        // Display as Fraction
	IsForQuest = 0x1000                // Is For Quest
}