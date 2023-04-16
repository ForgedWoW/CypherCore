// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Reputation;

public class FactionState
{
    public ReputationFlags Flags { get; set; }
    public uint Id { get; set; }
    public bool NeedSave { get; set; }
    public bool NeedSend { get; set; }
    public uint ReputationListID { get; set; }
    public int Standing { get; set; }
    public int VisualStandingIncrease { get; set; }
}