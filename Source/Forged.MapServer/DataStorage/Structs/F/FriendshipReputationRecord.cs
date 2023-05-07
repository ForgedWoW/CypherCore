// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.F;

public sealed record FriendshipReputationRecord
{
    public LocalizedString Description;
    public int FactionID;
    public FriendshipReputationFlags Flags;
    public uint Id;
    public LocalizedString StandingChanged;
    public LocalizedString StandingModified;
    public int TextureFileID;
}