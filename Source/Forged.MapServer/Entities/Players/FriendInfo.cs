// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class FriendInfo
{
    public ObjectGuid WowAccountGuid;
    public FriendStatus Status;
    public SocialFlag Flags;
    public uint Area;
    public uint Level;
    public PlayerClass Class;
    public string Note;

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