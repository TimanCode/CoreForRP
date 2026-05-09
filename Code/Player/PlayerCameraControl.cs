using Sandbox;

public sealed class PlayerCameraControl : Component
{
	[Property] public GameObject Target { get; set; }
	[Property] public Vector3 Offset { get; set; } = new Vector3( 0, 0, 60 );
	[Property] public float Distance { get; set; } = 200f;
	[Property] public float Sensitivity { get; set; } = 0.5f;
	[Property] public float SmoothTime { get; set; } = 0.15f;

	private Angles _angles;
	private Vector3 _smoothVelocity;

	protected override void OnStart()
	{
		if ( Target == null ) Target = GameObject;
		_angles = new Angles( 20, 0, 0 );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		var cam = Scene.Camera;
		if ( cam == null || Target == null ) return;

		// Ввод мыши
		var mouseDelta = Input.MouseDelta;
		_angles.yaw -= mouseDelta.x * Sensitivity;
		_angles.pitch += mouseDelta.y * Sensitivity;
		_angles.pitch = _angles.pitch.Clamp( -30f, 80f );

		// Расчет позиции
		Rotation targetRotation = _angles.ToRotation();
		Vector3 targetPosition = Target.WorldPosition + Offset + targetRotation.Backward * Distance;

		// Плавное перемещение (WorldPosition вместо Transform.Position)
		cam.WorldPosition = Vector3.SmoothDamp(
			cam.WorldPosition,
			targetPosition,
			ref _smoothVelocity,
			SmoothTime,
			Time.Delta
		);

		// Вращение (WorldRotation вместо Transform.Rotation)
		cam.WorldRotation = Rotation.LookAt( (Target.WorldPosition + Offset) - cam.WorldPosition, Vector3.Up );
		
		// Обновляем углы в скрипте движения для синхронизации головы
		var movement = Components.Get<PlayerMovementControl>();
		if ( movement != null )
		{
			movement.EyeAngles = _angles;
		}
	}
}