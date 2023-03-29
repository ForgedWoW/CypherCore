// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.WorldState;

public class InitWorldStates : ServerPacket
{
    public uint AreaID;
    public uint SubareaID;
    public uint MapID;
    private readonly List<WorldStateInfo> Worldstates = new();
    public InitWorldStates() : base(ServerOpcodes.InitWorldStates, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(MapID);
        _worldPacket.WriteUInt32(AreaID);
        _worldPacket.WriteUInt32(SubareaID);

        _worldPacket.WriteInt32(Worldstates.Count);

        foreach (var wsi in Worldstates)
        {
            _worldPacket.WriteUInt32(wsi.VariableID);
            _worldPacket.WriteInt32(wsi.Value);
        }
    }

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

    private struct WorldStateInfo
    {
        public WorldStateInfo(uint variableID, int value)
        {
            VariableID = variableID;
            Value = value;
        }

        public readonly uint VariableID;
        public readonly int Value;
    }
}