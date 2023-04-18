// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class FriendInfo
{
    public FriendInfo()
    {
        Status = FriendStatus.Offline;
        Note = "";
    }

    public FriendInfo(ObjectGuid accountGuid, SocialFlag flags, string note)
    {
        WowAccountGuid = accountGuid;
        Status = FriendStatus.Offline;
        Flags = flags;
        Note = note;
    }

    public uint Area { get; set; }
    public PlayerClass Class { get; set; }
    public SocialFlag Flags { get; set; }
    public uint Level { get; set; }
    public string Note { get; set; }
    public FriendStatus Status { get; set; }
    public ObjectGuid WowAccountGuid { get; set; }
}