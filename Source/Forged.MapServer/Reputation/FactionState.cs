// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Reputation;

public class FactionState
{
    public ReputationFlags Flags;
    public uint Id;
    public bool needSave;
    public bool needSend;
    public uint ReputationListID;
    public int Standing;
    public int VisualStandingIncrease;
}