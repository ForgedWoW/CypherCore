using Game.Networking;

namespace Game.Entities;

public class RestInfo : BaseUpdateData<Player>
{
	public UpdateField<uint> Threshold = new(0, 1);
	public UpdateField<byte> StateID = new(0, 2);

	public RestInfo() : base(3) { }

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteUInt32(Threshold);
		data.WriteUInt8(StateID);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		UpdateMask changesMask = ChangesMask;
		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 3);

		data.FlushBits();
		if (changesMask[0])
		{
			if (changesMask[1])
			{
				data.WriteUInt32(Threshold);
			}
			if (changesMask[2])
			{
				data.WriteUInt8(StateID);
			}
		}
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Threshold);
		ClearChangesMask(StateID);
		ChangesMask.ResetAll();
	}
}