// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Achievements;

public class AchievementReward
{
    public string Body { get; set; }
    public uint ItemId { get; set; }
    public uint MailTemplateId { get; set; }
    public uint SenderCreatureId { get; set; }
    public string Subject { get; set; }
    public uint[] TitleId { get; set; } = new uint[2];
}