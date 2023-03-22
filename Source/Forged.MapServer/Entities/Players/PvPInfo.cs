﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Entities;

public struct PvPInfo
{
	public bool IsHostile;
	public bool IsInHostileArea; //> Marks if player is in an area which forces PvP flag
	public bool IsInNoPvPArea;   //> Marks if player is in a sanctuary or friendly capital city
	public bool IsInFfaPvPArea;  //> Marks if player is in an FFAPvP area (such as Gurubashi Arena)
	public long EndTimer;        //> Time when player unflags himself for PvP (flag removed after 5 minutes)
}