using Sandbox;
using Sandbox.Citizen;

public sealed class PlayerMovementControl : Component
{
	[Property] public float MoveSpeed { get; set; } = 150f;
	[Property] public float RunSpeed { get; set; } = 300f;
	[Property] public float Acceleration { get; set; } = 10f;
	[Property] public float RotationSmoothTime { get; set; } = 10f;
	[Property] public float JumpPower { get; set; } = 300f;

	[Property] public GameObject Body { get; set; }

	[RequireComponent] public CharacterController Controller { get; set; }
	[RequireComponent] public CitizenAnimationHelper Animator { get; set; }

	private Vector3 _wishVelocity;
	private bool _isRunning;
	private bool _doJump;

	[Sync] public bool IsCrouching { get; set; }
	
	// ★ НОВОЕ: Синхронизируем поворот тела по сети
	[Sync] public Rotation BodyRotation { get; set; }

	[Sync] public Angles EyeAngles { get; set; }

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			HandleInput();
			CalculateRotation(); // Считаем поворот только у себя
		}

		// ★ ВАЖНО: Применяем поворот тела И анимации для всех (и для Proxy тоже)
		ApplyRotation();
		UpdateAnimation();
	}

	protected override void OnFixedUpdate()
	{
		if ( Controller == null || IsProxy ) return;

		ApplyMovement();
	}

	private void HandleInput()
	{
		var moveInput = Vector3.Zero;
		if ( Input.Down( "forward" ) ) moveInput += Vector3.Forward;
		if ( Input.Down( "backward" ) ) moveInput += Vector3.Backward;
		if ( Input.Down( "left" ) ) moveInput += Vector3.Left;
		if ( Input.Down( "right" ) ) moveInput += Vector3.Right;

		if ( moveInput.Length > 0 ) moveInput = moveInput.Normal;

		_isRunning = Input.Down( "run" );
		float currentMaxSpeed = IsCrouching ? 80f : (_isRunning ? RunSpeed : MoveSpeed);

		if ( Scene.Camera != null )
		{
			var camRot = Rotation.FromYaw( Scene.Camera.WorldRotation.Yaw() );
			_wishVelocity = camRot * moveInput * currentMaxSpeed;
		}

		_wishVelocity = _wishVelocity.WithZ( 0 );

		if ( Input.Pressed( "jump" ) && Controller.IsOnGround )
		{
			_doJump = true;
		}

		IsCrouching = Input.Down( "duck" );
	}

	// ★ НОВОЕ: Локальный расчет целевого поворота
	private void CalculateRotation()
	{
		if ( Body == null ) return;

		Vector3 moveDir = _wishVelocity.WithZ( 0 );
		if ( moveDir.Length > 0.01f )
		{
			// Сохраняем результат в синхронизируемую переменную
			BodyRotation = Rotation.LookAt( moveDir.Normal, Vector3.Up );
		}
	}

	// ★ НОВОЕ: Применение поворота для всех клиентов
	private void ApplyRotation()
	{
		if ( Body == null ) return;

		// Интерполяция (Slerp) теперь работает у всех игроков, 
		// основываясь на синхронизированном значении BodyRotation
		Body.WorldRotation = Rotation.Slerp( Body.WorldRotation, BodyRotation, Time.Delta * RotationSmoothTime );
	}

	private void ApplyMovement()
	{
		Vector3 horizVelocity = Controller.Velocity.WithZ( 0 );
		horizVelocity = Vector3.Lerp( horizVelocity, _wishVelocity, Acceleration * Time.Delta );

		float savedZ = Controller.Velocity.z;
		Controller.Velocity = horizVelocity.WithZ( savedZ );

		if ( _doJump && Controller.IsOnGround )
		{
			Controller.Punch( Vector3.Up * JumpPower );
			_doJump = false;
			BroadcastJump();
		}

		Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		Controller.Move();
	}

	[Rpc.Broadcast]
	private void BroadcastJump()
	{
		Animator?.TriggerJump();
	}

	private void UpdateAnimation()
	{
		if ( Animator == null ) return;

		Animator.WithVelocity( Controller.Velocity );
		Animator.WithWishVelocity( _wishVelocity );
		Animator.IsGrounded = Controller.IsOnGround;
		Animator.DuckLevel = IsCrouching ? 1f : 0f;
        
        // Передаем EyeAngles аниматору, чтобы голова тоже крутилась по сети
        Animator.WithLook( EyeAngles.Forward );
	}
}