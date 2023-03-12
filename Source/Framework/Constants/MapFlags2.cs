// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum MapFlags2 : uint
{
	DontActivateShowMap = 0x01,
	NoVoteKicks = 0x02,
	NoIncomingTransfers = 0x04,
	DontVoxelizePathData = 0x08,
	TerrainLOD = 0x10,
	UnclampedPointLights = 0x20,
	PVP = 0x40,
	IgnoreInstanceFarmLimit = 0x80,
	DontInheritAreaLightsFromParent = 0x100,
	ForceLightBufferOn = 0x200,
	WMOLiquidScale = 0x400,
	SpellClutterOn = 0x800,
	SpellClutterOff = 0x1000,
	ReducedPathMapHeightValidation = 0x2000,
	NewMinimapGeneration = 0x4000,
	AIBotsDetectedLikePlayers = 0x8000,
	LinearlyLitTerrain = 0x10000,
	FogOfWar = 0x20000,
	DisableSharedWeatherSystems = 0x40000,
	HonorSpellAttribute11LosHitsNocamcollide = 0x80000,
	BelongsToLayer = 0x100000,
}