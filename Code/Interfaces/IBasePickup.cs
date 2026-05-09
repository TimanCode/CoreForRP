using Sandbox;

// Рекомендую положить это в папку типа code/Core/Interfaces
public interface IInteractable
{
	// Передаем GameObject игрока, чтобы знать, КТО именно нажал кнопку
	void OnUse( GameObject player );
}