// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Players;

public struct PvPInfo
{
    public long EndTimer;
    public bool IsHostile;
    public bool IsInFfaPvPArea;
    public bool IsInHostileArea; //> Marks if player is in an area which forces PvP flag
    public bool IsInNoPvPArea;   //> Marks if player is in a sanctuary or friendly capital city
      //> Marks if player is in an FFAPvP area (such as Gurubashi Arena)
            //> Time when player unflags himself for PvP (flag removed after 5 minutes)
}