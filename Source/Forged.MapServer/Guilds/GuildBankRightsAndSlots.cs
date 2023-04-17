// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Guilds;

public class GuildBankRightsAndSlots
{
    public GuildBankRightsAndSlots(byte tabId = 0xFF, sbyte rights = 0, int slots = 0)
    {
        TabId = tabId;
        Rights = (GuildBankRights)rights;
        Slots = slots;
    }

    public GuildBankRights Rights { get; set; }

    public int Slots { get; set; }

    public byte TabId { get; set; }

    public void SetGuildMasterValues()
    {
        Rights = GuildBankRights.Full;
        Slots = Convert.ToInt32(GuildConst.WithdrawSlotUnlimited);
    }
}