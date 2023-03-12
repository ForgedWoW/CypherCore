// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GameObjectActions
{
	// Name from client executable          // Comments
	None = 0,                         // -NONE-
	AnimateCustom0 = 1,               // Animate Custom0
	AnimateCustom1 = 2,               // Animate Custom1
	AnimateCustom2 = 3,               // Animate Custom2
	AnimateCustom3 = 4,               // Animate Custom3
	Disturb = 5,                      // Disturb                              // Triggers trap
	Unlock = 6,                       // Unlock                               // Resets GO_FLAG_LOCKED
	Lock = 7,                         // Lock                                 // Sets GO_FLAG_LOCKED
	Open = 8,                         // Open                                 // Sets GO_STATE_ACTIVE
	OpenAndUnlock = 9,                // Open + Unlock                        // Sets GO_STATE_ACTIVE and resets GO_FLAG_LOCKED
	Close = 10,                       // Close                                // Sets GO_STATE_READY
	ToggleOpen = 11,                  // Toggle Open
	Destroy = 12,                     // Destroy                              // Sets GO_STATE_DESTROYED
	Rebuild = 13,                     // Rebuild                              // Resets from GO_STATE_DESTROYED
	Creation = 14,                    // Creation
	Despawn = 15,                     // Despawn
	MakeInert = 16,                   // Make Inert                           // Disables interactions
	MakeActive = 17,                  // Make Active                          // Enables interactions
	CloseAndLock = 18,                // Close + Lock                         // Sets GO_STATE_READY and sets GO_FLAG_LOCKED
	UseArtKit0 = 19,                  // Use ArtKit0                          // 46904: 121
	UseArtKit1 = 20,                  // Use ArtKit1                          // 36639: 81, 46903: 122
	UseArtKit2 = 21,                  // Use ArtKit2
	UseArtKit3 = 22,                  // Use ArtKit3
	SetTapList = 23,                  // Set Tap List
	GoTo1stFloor = 24,                // Go to 1st floor
	GoTo2ndFloor = 25,                // Go to 2nd floor
	GoTo3rdFloor = 26,                // Go to 3rd floor
	GoTo4thFloor = 27,                // Go to 4th floor
	GoTo5thFloor = 28,                // Go to 5th floor
	GoTo6thFloor = 29,                // Go to 6th floor
	GoTo7thFloor = 30,                // Go to 7th floor
	GoTo8thFloor = 31,                // Go to 8th floor
	GoTo9thFloor = 32,                // Go to 9th floor
	GoTo10thFloor = 33,               // Go to 10th floor
	UseArtKit4 = 34,                  // Use ArtKit4
	PlayAnimKit = 35,                 // Play Anim Kit "{AnimKit}"
	OpenAndPlayAnimKit = 36,          // Open + Play Anim Kit "{AnimKit}"
	CloseAndPlayAnimKit = 37,         // Close + Play Anim Kit "{AnimKit}"
	PlayOneShotAnimKit = 38,          // Play One-shot Anim Kit "{AnimKit}"
	StopAnimKit = 39,                 // Stop Anim Kit
	OpenAndStopAnimKit = 40,          // Open + Stop Anim Kit
	CloseAndStopAnimKit = 41,         // Close + Stop Anim Kit
	PlaySpellVisual = 42,             // Play Spell Visual "{SpellVisual}"
	StopSpellVisual = 43,             // Stop Spell Visual
	SetTappedToChallengePlayers = 44, // Set Tapped to Challenge Players
	Max
}