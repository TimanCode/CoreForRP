using Sandbox;

public interface IInteractable
{
	string DisplayName { get; }
	void OnUse( GameObject player );
}