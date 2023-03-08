namespace Game.Entities.GameObjectType;

class SetTransportAutoCycleBetweenStopFrames : GameObjectTypeBase.CustomCommand
{
	readonly bool _on;

	public SetTransportAutoCycleBetweenStopFrames(bool on)
	{
		_on = on;
	}

	public override void Execute(GameObjectTypeBase type)
	{
		Transport transport = (Transport)type;
		if (transport != null)
			transport.SetAutoCycleBetweenStopFrames(_on);
	}
}