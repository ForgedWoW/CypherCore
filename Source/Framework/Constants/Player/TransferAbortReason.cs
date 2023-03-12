// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum TransferAbortReason
{
	None = 0,
	Error = 1,
	MaxPlayers = 2,                  // Transfer ed: Instance Is Full
	NotFound = 3,                    // Transfer ed: Instance Not Found
	TooManyInstances = 4,            // You Have Entered Too Many Instances Recently.
	ZoneInCombat = 6,                // Unable To Zone In While An Encounter Is In Progress.
	InsufExpanLvl = 7,               // You Must Have <Tbc, Wotlk> Expansion Installed To Access This Area.
	Difficulty = 8,                  // <Normal, Heroic, Epic> Difficulty Mode Is Not Available For %S.
	UniqueMessage = 9,               // Until You'Ve Escaped Tlk'S Grasp, You Cannot Leave This Place!
	TooManyRealmInstances = 10,      // Additional Instances Cannot Be Launched, Please Try Again Later.
	NeedGroup = 11,                  // Transfer ed: You Must Be In A Raid Group To Enter This Instance
	NotFound2 = 12,                  // Transfer ed: Instance Not Found
	NotFound3 = 13,                  // Transfer ed: Instance Not Found
	NotFound4 = 14,                  // Transfer ed: Instance Not Found
	RealmOnly = 15,                  // All Players In The Party Must Be From The Same Realm To Enter %S.
	MapNotAllowed = 16,              // Map Cannot Be Entered At This Time.
	LockedToDifferentInstance = 18,  // You Are Already Locked To %S
	AlreadyCompletedEncounter = 19,  // You Are Ineligible To Participate In At Least One Encounter In This Instance Because You Are Already Locked To An Instance In Which It Has Been Defeated.
	DifficultyNotFound = 22,         // Client Writes To Console "Unable To Resolve Requested Difficultyid %U To Actual Difficulty For Map %D"
	XrealmZoneDown = 24,             // Transfer ed: Cross-Realm Zone Is Down
	SoloPlayerSwitchDifficulty = 26, // This Instance Is Already In Progress. You May Only Switch Difficulties From Inside The Instance.
	NotCrossFactionCompatible = 33,  // This instance isn't available for cross-faction groups
}