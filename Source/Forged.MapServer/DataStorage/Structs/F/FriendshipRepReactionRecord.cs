// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.F;

public sealed record FriendshipRepReactionRecord
{
    public uint FriendshipRepID;
    public uint Id;
    public int OverrideColor;
    public LocalizedString Reaction;
    public ushort ReactionThreshold;
}