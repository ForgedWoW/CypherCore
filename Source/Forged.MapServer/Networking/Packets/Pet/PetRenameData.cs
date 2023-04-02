// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Networking.Packets.Pet;

internal struct PetRenameData
{
    public DeclinedName DeclinedNames;
    public bool HasDeclinedNames;
    public string NewName;
    public ObjectGuid PetGUID;
    public int PetNumber;
}