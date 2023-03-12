// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.BattleGrounds;

namespace Game.Entities;

public class BgBattlegroundQueueIdRec
{
	public BattlegroundQueueTypeId BgQueueTypeId { get; set; }
	public uint InvitedToInstance { get; set; }
	public uint JoinTime { get; set; }
	public bool Mercenary { get; set; }
}