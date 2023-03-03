﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.IO;

namespace Game.Networking.Packets
{
    class Notification : ServerPacket
    {
        public Notification() : base(ServerOpcodes.BattlenetNotification) { }

        public override void Write()
        {
            Method.Write(_worldPacket);
            _worldPacket.WriteUInt32(Data.GetSize());
            _worldPacket.WriteBytes(Data);
        }

        public MethodCall Method;
        public ByteBuffer Data = new();
    }

    class Response : ServerPacket
    {
        public Response() : base(ServerOpcodes.BattlenetResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32((uint)BnetStatus);
            Method.Write(_worldPacket);
            _worldPacket.WriteUInt32(Data.GetSize());
            _worldPacket.WriteBytes(Data);
        }

        public BattlenetRpcErrorCode BnetStatus = BattlenetRpcErrorCode.Ok;
        public MethodCall Method;
        public ByteBuffer Data = new();
    }

    class ConnectionStatus : ServerPacket
    {
        public ConnectionStatus() : base(ServerOpcodes.BattleNetConnectionStatus) { }

        public override void Write()
        {
            _worldPacket.WriteBits(State, 2);
            _worldPacket.WriteBit(SuppressNotification);
            _worldPacket.FlushBits();
        }

        public byte State;
        public bool SuppressNotification = true;
    }

    class ChangeRealmTicketResponse : ServerPacket
    {
        public ChangeRealmTicketResponse() : base(ServerOpcodes.ChangeRealmTicketResponse) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Token);
            _worldPacket.WriteBit(Allow);
            _worldPacket.WriteUInt32(Ticket.GetSize());
            _worldPacket.WriteBytes(Ticket);
        }

        public uint Token;
        public bool Allow = true;
        public ByteBuffer Ticket;
    }

    class BattlenetRequest : ClientPacket
    {
        public BattlenetRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Method.Read(_worldPacket);
            uint protoSize = _worldPacket.ReadUInt32();

            Data = _worldPacket.ReadBytes(protoSize);
        }

        public MethodCall Method;
        public byte[] Data;
    }

    class ChangeRealmTicket : ClientPacket
    {
        public ChangeRealmTicket(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Token = _worldPacket.ReadUInt32();
            for (var i = 0; i < Secret.GetLimit(); ++i)
                Secret[i] = _worldPacket.ReadUInt8();
        }

        public uint Token;
        public Array<byte> Secret = new(32);
    }

    public struct MethodCall
    {
        public uint GetServiceHash() { return (uint)(Type >> 32); }
        public uint GetMethodId() { return (uint)(Type & 0xFFFFFFFF); }

        public void Read(ByteBuffer data)
        {
            Type = data.ReadUInt64();
            ObjectId = data.ReadUInt64();
            Token = data.ReadUInt32();
        }

        public void Write(ByteBuffer data)
        {
            data.WriteUInt64(Type);
            data.WriteUInt64(ObjectId);
            data.WriteUInt32(Token);
        }

        public ulong Type;
        public ulong ObjectId;
        public uint Token;
    }
}
