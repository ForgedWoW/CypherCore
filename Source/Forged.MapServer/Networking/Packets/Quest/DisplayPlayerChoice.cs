// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

internal class DisplayPlayerChoice : ServerPacket
{
    public int ChoiceID;
    public bool CloseChoiceFrame;
    public uint CloseUISoundKitID;
    public long Duration;
    public bool HideWarboardHeader;
    public bool KeepOpenAfterChoice;
    public byte NumRerolls;
    public string PendingChoiceText;
    public string Question;
    public List<PlayerChoiceResponse> Responses = new();
    public ObjectGuid SenderGUID;
    public uint SoundKitID;
    public int UiTextureKitID;
    public DisplayPlayerChoice() : base(ServerOpcodes.DisplayPlayerChoice) { }

    public override void Write()
    {
        _worldPacket.WriteInt32(ChoiceID);
        _worldPacket.WriteInt32(Responses.Count);
        _worldPacket.WritePackedGuid(SenderGUID);
        _worldPacket.WriteInt32(UiTextureKitID);
        _worldPacket.WriteUInt32(SoundKitID);
        _worldPacket.WriteUInt32(CloseUISoundKitID);
        _worldPacket.WriteUInt8(NumRerolls);
        _worldPacket.WriteInt64(Duration);
        _worldPacket.WriteBits(Question.GetByteCount(), 8);
        _worldPacket.WriteBits(PendingChoiceText.GetByteCount(), 8);
        _worldPacket.WriteBit(CloseChoiceFrame);
        _worldPacket.WriteBit(HideWarboardHeader);
        _worldPacket.WriteBit(KeepOpenAfterChoice);
        _worldPacket.FlushBits();

        foreach (var response in Responses)
            response.Write(_worldPacket);

        _worldPacket.WriteString(Question);
        _worldPacket.WriteString(PendingChoiceText);
    }
}