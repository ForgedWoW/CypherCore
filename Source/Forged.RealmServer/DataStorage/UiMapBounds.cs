// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

class UiMapBounds
{
	// these coords are mixed when calculated and used... its a mess
	public float[] Bounds = new float[4];
	public bool IsUiAssignment;
	public bool IsUiLink;
}