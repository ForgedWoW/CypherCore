namespace Game.Entities;

public struct PassengerInfo
{
	public ObjectGuid Guid;
	public bool IsUninteractible;
	public bool IsGravityDisabled;

	public void Reset()
	{
		Guid = ObjectGuid.Empty;
		IsUninteractible = false;
		IsGravityDisabled = false;
	}
}