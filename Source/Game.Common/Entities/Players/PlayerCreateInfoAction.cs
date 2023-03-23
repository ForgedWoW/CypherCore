// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Players;
using Game.Entities;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Entities.Players;

public class PlayerCreateInfoAction
{
	public byte Button { get; set; }
	public byte Type { get; set; }
	public uint Action { get; set; }

	public PlayerCreateInfoAction() : this(0, 0, 0) { }

	public PlayerCreateInfoAction(byte button, uint action, byte type)
	{
		Button = button;
		Type = type;
		Action = action;
	}
}
