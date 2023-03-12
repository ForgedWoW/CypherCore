// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public class MapConst
{
	public const uint InvalidZone = 0xFFFFFFFF;

	//Grids
	public const int MaxGrids = 64;
	public const float SizeofGrids = 533.33333f;
	public const int CenterGridCellId = (MaxCells * MaxGrids / 2);
	public const int CenterGridId = (MaxGrids / 2);
	public const float CenterGridOffset = (SizeofGrids / 2);
	public const float CenterGridCellOffset = (SizeofCells / 2);

	//Cells
	public const int MaxCells = 8;
	public const float SizeofCells = (SizeofGrids / MaxCells);
	public const int TotalCellsPerMap = (MaxGrids * MaxCells);
	public const float MapSize = (SizeofGrids * MaxGrids);
	public const float MapHalfSize = (MapSize / 2);

	public const uint MaxGroupSize = 5;
	public const uint MaxRaidSize = 40;
	public const uint MaxRaidSubGroups = MaxRaidSize / MaxGroupSize;
	public const uint TargetIconsCount = 8;
	public const uint RaidMarkersCount = 8;
	public const uint ReadycheckDuration = 35000;

	//Liquid
	public const float LiquidTileSize = (533.333f / 128.0f);

	public const int MinMapUpdateDelay = 1;
	public const int MinGridDelay = (Time.Minute * Time.InMilliseconds);

	public const int MapResolution = 128;
	public const float DefaultHeightSearch = 50.0f;
	public const float InvalidHeight = -100000.0f;
	public const float MaxHeight = 100000.0f;
	public const float MaxFallDistance = 250000.0f;
	public const float GroundHeightTolerance = 0.05f;
	public const float ZOffsetFindHeight = 0.5f;
	public const float DefaultCollesionHeight = 2.03128f; // Most common value in dbc

	public const uint MapMagic = 0x5350414D; //"MAPS";
	public const uint MapVersionMagic = 10;
	public const uint MapVersionMagic2 = 0x302E3276; //"v2.0"; // Hack for some different extractors using v2.0 header
	public const uint MapAreaMagic = 0x41455241;     //"AREA";
	public const uint MapHeightMagic = 0x5447484D;   //"MHGT";
	public const uint MapLiquidMagic = 0x51494C4D;   //"MLIQ";

	public const uint mmapMagic = 0x4D4D4150; // 'MMAP'
	public const int mmapVersion = 15;

	public const string VMapMagic = "VMAP_4.B";
	public const float VMAPInvalidHeightValue = -200000.0f;

	public const uint MaxDungeonEncountersPerBoss = 4;
}