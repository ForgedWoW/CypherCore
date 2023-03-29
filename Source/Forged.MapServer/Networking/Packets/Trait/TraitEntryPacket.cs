// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects.Update;

namespace Forged.MapServer.Networking.Packets.Trait;

public class TraitEntryPacket
{
    public int TraitNodeID;
    public int TraitNodeEntryID;
    public int Rank;
    public int GrantedRanks;

    public TraitEntryPacket() { }

    public TraitEntryPacket(TraitEntry ufEntry)
    {
        TraitNodeID = ufEntry.TraitNodeID;
        TraitNodeEntryID = ufEntry.TraitNodeEntryID;
        Rank = ufEntry.Rank;
        GrantedRanks = ufEntry.GrantedRanks;
    }

    public void Read(WorldPacket data)
    {
        TraitNodeID = data.ReadInt32();
        TraitNodeEntryID = data.ReadInt32();
        Rank = data.ReadInt32();
        GrantedRanks = data.ReadInt32();
    }

    public void Write(WorldPacket data)
    {
        data.WriteInt32(TraitNodeID);
        data.WriteInt32(TraitNodeEntryID);
        data.WriteInt32(Rank);
        data.WriteInt32(GrantedRanks);
    }
}