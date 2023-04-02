// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Players;

public class PlayerCreateInfoAction
{
    public PlayerCreateInfoAction() : this(0, 0, 0) { }

    public PlayerCreateInfoAction(byte button, uint action, byte type)
    {
        Button = button;
        Type = type;
        Action = action;
    }

    public uint Action { get; set; }
    public byte Button { get; set; }
    public byte Type { get; set; }
}