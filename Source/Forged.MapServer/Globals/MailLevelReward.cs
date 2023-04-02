// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Globals;

public class MailLevelReward
{
    public uint MailTemplateId;
    public ulong RaceMask;
    public uint SenderEntry;

    public MailLevelReward(ulong raceMask = 0, uint mailTemplateId = 0, uint senderEntry = 0)
    {
        RaceMask = raceMask;
        MailTemplateId = mailTemplateId;
        SenderEntry = senderEntry;
    }
}