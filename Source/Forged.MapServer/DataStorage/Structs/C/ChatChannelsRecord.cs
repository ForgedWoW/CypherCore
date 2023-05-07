// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ChatChannelsRecord
{
    public sbyte FactionGroup;
    public ChannelDBCFlags Flags;
    public uint Id;
    public LocalizedString Name;
    public int Ruleset;
    public string Shortcut;
}