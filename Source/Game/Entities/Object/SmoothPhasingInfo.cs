// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Entities;

public class SmoothPhasingInfo
{
	// Fields visible on client
	public ObjectGuid? ReplaceObject;

	public SmoothPhasingInfo(ObjectGuid replaceObject, bool replaceActive, bool stopAnimKits)
	{
		ReplaceObject = replaceObject;
		ReplaceActive = replaceActive;
		StopAnimKits  = stopAnimKits;
	}

	public bool ReplaceActive { get; set; } = true;
	public bool StopAnimKits { get; set; } = true;

	// Serverside fields
	public bool Disabled { get; set; } = false;
}