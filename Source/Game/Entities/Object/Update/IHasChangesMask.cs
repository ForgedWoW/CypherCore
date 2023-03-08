namespace Game.Entities;

public interface IHasChangesMask
{
	void ClearChangesMask();
	UpdateMask GetUpdateMask();
}