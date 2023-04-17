// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Scripts.Spells.DemonHunter;

public class AuraData
{
    public uint MID;
    public ObjectGuid MCasterGuid = new();

    public AuraData(uint id, ObjectGuid casterGUID)
    {
        MID = id;
        MCasterGuid = casterGUID;
    }
}