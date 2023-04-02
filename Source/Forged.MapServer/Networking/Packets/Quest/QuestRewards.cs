// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestRewards
{
    public uint ArtifactCategoryID;
    public uint ArtifactXP;
    public uint ChoiceItemCount;
    public QuestChoiceItem[] ChoiceItems = new QuestChoiceItem[SharedConst.QuestRewardChoicesCount];
    public uint[] CurrencyID = new uint[SharedConst.QuestRewardCurrencyCount];
    public uint[] CurrencyQty = new uint[SharedConst.QuestRewardCurrencyCount];
    public int[] FactionCapIn = new int[SharedConst.QuestRewardReputationsCount];
    public uint FactionFlags;
    public uint[] FactionID = new uint[SharedConst.QuestRewardReputationsCount];
    public int[] FactionOverride = new int[SharedConst.QuestRewardReputationsCount];
    public int[] FactionValue = new int[SharedConst.QuestRewardReputationsCount];
    public uint Honor;
    public bool IsBoostSpell;
    public uint ItemCount;
    public uint[] ItemID = new uint[SharedConst.QuestRewardItemCount];
    public uint[] ItemQty = new uint[SharedConst.QuestRewardItemCount];
    public uint Money;
    public uint NumSkillUps;
    public uint SkillLineID;
    public int[] SpellCompletionDisplayID = new int[SharedConst.QuestRewardDisplaySpellCount];
    public uint SpellCompletionID;
    public uint Title;
    public uint TreasurePickerID;
    public uint XP;
    public void Write(WorldPacket data)
    {
        data.WriteUInt32(ChoiceItemCount);
        data.WriteUInt32(ItemCount);

        for (var i = 0; i < SharedConst.QuestRewardItemCount; ++i)
        {
            data.WriteUInt32(ItemID[i]);
            data.WriteUInt32(ItemQty[i]);
        }

        data.WriteUInt32(Money);
        data.WriteUInt32(XP);
        data.WriteUInt64(ArtifactXP);
        data.WriteUInt32(ArtifactCategoryID);
        data.WriteUInt32(Honor);
        data.WriteUInt32(Title);
        data.WriteUInt32(FactionFlags);

        for (var i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
        {
            data.WriteUInt32(FactionID[i]);
            data.WriteInt32(FactionValue[i]);
            data.WriteInt32(FactionOverride[i]);
            data.WriteInt32(FactionCapIn[i]);
        }

        foreach (var id in SpellCompletionDisplayID)
            data.WriteInt32(id);

        data.WriteUInt32(SpellCompletionID);

        for (var i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
        {
            data.WriteUInt32(CurrencyID[i]);
            data.WriteUInt32(CurrencyQty[i]);
        }

        data.WriteUInt32(SkillLineID);
        data.WriteUInt32(NumSkillUps);
        data.WriteUInt32(TreasurePickerID);

        foreach (var choice in ChoiceItems)
            choice.Write(data);

        data.WriteBit(IsBoostSpell);
        data.FlushBits();
    }
}