using Sandbox;

public sealed class DrinkPickup : BasePickup
{
	[Property, Group( "Напиток" )] 
	public float ThirstAmount { get; set; } = 25f; // Сколько жажды восстанавливает

	public override void OnConsume( GameObject player )
	{
		var stats = player.Components.Get<PlayerStats>();
		if ( stats != null )
		{
			// Пополняем жажду
			stats.AddThirst( ThirstAmount );

			// Опционально: здесь можно добавить воспроизведение звука глотка
			// Sound.Play( "sounds/player/drink.sound", player.Transform.Position );
		}
	}
}