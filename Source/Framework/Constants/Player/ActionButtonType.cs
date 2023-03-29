// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ActionButtonType
{
    Spell = 0x00,
    C = 0x01, // click?
    Eqset = 0x20,
    Dropdown = 0x30,
    Macro = 0x40,
    CMacro = C | Macro,
    Companion = 0x50,
    Mount = 0x60,
    Item = 0x80
}