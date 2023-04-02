// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class ActionButton
{
    public ActionButtonUpdateState UState;

    public ActionButton()
    {
        PackedData = 0;
        UState = ActionButtonUpdateState.New;
    }

    public ulong PackedData { get; set; }
    public ulong GetAction()
    {
        return (PackedData & 0x00FFFFFFFFFFFFFF);
    }

    public ActionButtonType GetButtonType()
    {
        return (ActionButtonType)((PackedData & 0xFF00000000000000) >> 56);
    }
    public void SetActionAndType(ulong action, ActionButtonType type)
    {
        var newData = action | ((ulong)type << 56);

        if (newData != PackedData || UState == ActionButtonUpdateState.Deleted)
        {
            PackedData = newData;

            if (UState != ActionButtonUpdateState.New)
                UState = ActionButtonUpdateState.Changed;
        }
    }
}