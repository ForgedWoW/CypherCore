// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.WorldState;

public class InitWorldStates : ServerPacket
{
    public uint AreaID;
    public uint MapID;
    public uint SubareaID;
    private readonly List<WorldStateInfo> Worldstates = new();
    public InitWorldStates() : base(ServerOpcodes.InitWorldStates, ConnectionType.Instance) { }

    public void AddState(WorldStates variableID, uint value)
    {
        AddState((uint)variableID, value);
    }

    public void AddState(uint variableID, uint value)
    {
        Worldstates.Add(new WorldStateInfo(variableID, (int)value));
    }

    public void AddState(int variableID, int value)
    {
        Worldstates.Add(new WorldStateInfo((uint)variableID, value));
    }

    public void AddState(WorldStates variableID, bool value)
    {
        AddState((uint)variableID, value);
    }

    public void AddState(uint variableID, bool value)
    {
        Worldstates.Add(new WorldStateInfo(variableID, value ? 1 : 0));
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32(MapID);
        WorldPacket.WriteUInt32(AreaID);
        WorldPacket.WriteUInt32(SubareaID);

        WorldPacket.WriteInt32(Worldstates.Count);

        foreach (var wsi in Worldstates)
        {
            WorldPacket.WriteUInt32(wsi.VariableID);
            WorldPacket.WriteInt32(wsi.Value);
        }
    }
    private struct WorldStateInfo
    {
        public readonly int Value;

        public readonly uint VariableID;

        public WorldStateInfo(uint variableID, int value)
        {
            VariableID = variableID;
            Value = value;
        }
    }
}