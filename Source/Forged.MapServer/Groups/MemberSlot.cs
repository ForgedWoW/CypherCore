// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Groups;

public class MemberSlot
{
    public byte Class;
    public GroupMemberFlags Flags;
    public byte Group;
    public ObjectGuid Guid;
    public string Name;
    public Race Race;
    public bool ReadyChecked;
    public LfgRoles Roles;
}