// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ChrRacesFlag
{
	NPCOnly = 0x01,
	DoNotComponentFeet = 0x02,
	CanMount = 0x04,
	HasBald = 0x08,
	BindToStartingArea = 0x10,
	AlternateForm = 0x20,
	CanMountSelf = 0x40,
	ForceToHDModelIfAvailable = 0x80,
	ExaltedWithAllVendors = 0x100,
	NotSelectable = 0x200,
	ReputationBonus = 0x400,
	UseLoincloth = 0x800,
	RestBonus = 0x1000,
	NoStartKits = 0x2000,
	NoStartingWeapon = 0x4000,
	DontRedeemAccountLicenses = 0x8000,
	SkinVariationIsHairColor = 0x10000,
	UsePandarenRingForComponentingTexture = 0x20000,
	IgnoreForAssetManifestComponentInfoParsing = 0x40000,
	IsAlliedRace = 0x80000,
	VoidVendorDiscount = 0x100000,
	DAMMComponentNoMaleGeneration = 0x200000,
	DAMMComponentNoFemaleGeneration = 0x400000,
	NoAssociatedFactionReputationInRaceChange = 0x800000,
	InternalOnly = 0x100000,
}