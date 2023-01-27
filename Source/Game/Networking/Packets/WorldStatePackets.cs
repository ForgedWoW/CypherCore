﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets
{
	public class InitWorldStates : ServerPacket
	{
		public uint AreaID;
		public uint MapID;
		public uint SubareaID;

		private List<WorldStateInfo> Worldstates = new();

		public InitWorldStates() : base(ServerOpcodes.InitWorldStates, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(MapID);
			_worldPacket.WriteUInt32(AreaID);
			_worldPacket.WriteUInt32(SubareaID);

			_worldPacket.WriteInt32(Worldstates.Count);

			foreach (WorldStateInfo wsi in Worldstates)
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
				Value      = value;
			}

			public uint VariableID;
			public int Value;
		}
	}

	public class UpdateWorldState : ServerPacket
	{
		public bool Hidden; // @todo: research

		public int Value;
		public uint VariableID;

		public UpdateWorldState() : base(ServerOpcodes.UpdateWorldState, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(VariableID);
			_worldPacket.WriteInt32(Value);
			_worldPacket.WriteBit(Hidden);
			_worldPacket.FlushBits();
		}
	}
}