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
        WorldPacket.WriteInt32(ChoiceID);
        WorldPacket.WriteInt32(Responses.Count);
        WorldPacket.WritePackedGuid(SenderGUID);
        WorldPacket.WriteInt32(UiTextureKitID);
        WorldPacket.WriteUInt32(SoundKitID);
        WorldPacket.WriteUInt32(CloseUISoundKitID);
        WorldPacket.WriteUInt8(NumRerolls);
        WorldPacket.WriteInt64(Duration);
        WorldPacket.WriteBits(Question.GetByteCount(), 8);
        WorldPacket.WriteBits(PendingChoiceText.GetByteCount(), 8);
        WorldPacket.WriteBit(CloseChoiceFrame);
        WorldPacket.WriteBit(HideWarboardHeader);
        WorldPacket.WriteBit(KeepOpenAfterChoice);
        WorldPacket.FlushBits();

        foreach (var response in Responses)
            response.Write(WorldPacket);

        WorldPacket.WriteString(Question);
        WorldPacket.WriteString(PendingChoiceText);
    }
}