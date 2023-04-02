// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class FriendInfo
{
    public uint Area;
    public PlayerClass Class;
    public SocialFlag Flags;
    public uint Level;
    public string Note;
    public FriendStatus Status;
    public ObjectGuid WowAccountGuid;
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
}