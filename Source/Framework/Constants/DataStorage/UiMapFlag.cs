// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum UiMapFlag
{
	None = 0x00,
	NoHighlight = 0x01,
	ShowOverlays = 0x02,
	ShowTaxiNodes = 0x04,
	GarrisonMap = 0x08,
	FallbackToParentMap = 0x10,
	NoHighlightTexture = 0x20,
	ShowTaskObjectives = 0x40,
	NoWorldPositions = 0x80,
	HideArchaeologyDigs = 0x100,
	Deprecated = 0x200,
	HideIcons = 0x400,
	HideVignettes = 0x800,
	ForceAllOverlayExplored = 0x1000,
	FlightMapShowZoomOut = 0x2000,
	FlightMapAutoZoom = 0x4000,
	ForceOnNavbar = 0x8000
}