// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class PageText
{
    public byte Flags { get; set; }
    public uint NextPageID { get; set; }
    public int PlayerConditionID { get; set; }
    public string Text { get; set; }
}