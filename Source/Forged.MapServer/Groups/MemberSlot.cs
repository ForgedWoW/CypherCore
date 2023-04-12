// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Groups;

public class MemberSlot
{
    public byte Class { get; set; }
    public GroupMemberFlags Flags { get; set; }
    public byte Group { get; set; }
    public ObjectGuid Guid { get; set; }
    public string Name { get; set; }
    public Race Race { get; set; }
    public bool ReadyChecked { get; set; }
    public LfgRoles Roles { get; set; }
}