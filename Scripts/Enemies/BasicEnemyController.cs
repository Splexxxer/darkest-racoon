using Godot;

public partial class BasicEnemyController : CharacterBody2D, IPlayerTargeted
{
	public const string EnemyGroupName = "Enemies";

	[Export] public float Speed = 120f;
	[Export] public float StopDistance = 0f;
	[Export(PropertyHint.Range, "0,500,1")] public float KnockbackDistance = 60f;
	[Export(PropertyHint.Range, "0,2,0.01")] public float KnockbackDuration = 0.15f;

	private Vector2 _pendingKnockbackMotion = Vector2.Zero;
	private float _pendingKnockbackDuration;
	private float _knockbackSpeed;

	public bool IsExperiencingKnockback => _pendingKnockbackMotion.LengthSquared() > 0.001f;

	public Node2D Target { get; set; }

	public override void _Ready()
	{
		base._Ready();
		if (!IsInGroup(EnemyGroupName))
			AddToGroup(EnemyGroupName);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_pendingKnockbackMotion.LengthSquared() > 0.001f)
		{
			ApplyPendingKnockback(delta);
			return;
		}

		Vector2 desiredVelocity = Vector2.Zero;

		if (Target != null)
		{
			Vector2 toPlayer = Target.GlobalPosition - GlobalPosition;
			float dist = toPlayer.Length();

			if (!(StopDistance > 0f && dist <= StopDistance))
			{
				Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.Zero;
				desiredVelocity = dir * Speed;
			}
		}

		Velocity = desiredVelocity;
		MoveAndSlide();
	}

	public void ApplyKnockback(Vector2 direction, float distanceOverride = -1f, float durationOverride = -1f)
	{
		float distance = distanceOverride > 0f ? distanceOverride : KnockbackDistance;
		if (distance <= 0f || direction.LengthSquared() < 0.0001f)
			return;

		_pendingKnockbackMotion = direction.Normalized() * distance;
		float duration = durationOverride > 0f ? durationOverride : KnockbackDuration;
		if (duration > 0f)
		{
			_pendingKnockbackDuration = duration;
			_knockbackSpeed = distance / duration;
		}
		else
		{
			_pendingKnockbackDuration = 0f;
			_knockbackSpeed = 0f;
		}
	}

	public void ApplySeparation(Vector2 offset)
	{
		if (offset == Vector2.Zero)
			return;

		ApplyImmediateMotion(offset);
	}

	private void ApplyPendingKnockback(double delta)
	{
		if (_pendingKnockbackMotion == Vector2.Zero)
			return;

		if (_pendingKnockbackDuration <= 0f || _knockbackSpeed <= 0f)
		{
			ApplyImmediateMotion(_pendingKnockbackMotion, true);
			_pendingKnockbackMotion = Vector2.Zero;
			return;
		}

		float remainingDistance = _pendingKnockbackMotion.Length();
		if (remainingDistance < 0.0001f)
		{
			_pendingKnockbackMotion = Vector2.Zero;
			return;
		}

		float stepDistance = Mathf.Min(remainingDistance, _knockbackSpeed * (float)delta);
		Vector2 motion = _pendingKnockbackMotion.Normalized() * stepDistance;
		ApplyImmediateMotion(motion, true);

		if (Mathf.IsZeroApprox(remainingDistance - stepDistance))
		{
			_pendingKnockbackMotion = Vector2.Zero;
			_pendingKnockbackDuration = 0f;
			_knockbackSpeed = 0f;
			return;
		}

		_pendingKnockbackMotion -= motion;
		_pendingKnockbackDuration = Mathf.Max(0f, _pendingKnockbackDuration - (float)delta);

		if (_pendingKnockbackDuration <= 0f)
		{
			ApplyImmediateMotion(_pendingKnockbackMotion, true);
			_pendingKnockbackMotion = Vector2.Zero;
		}
	}

	private void ApplyImmediateMotion(Vector2 motion, bool ignoreEnemyCollisions = false)
	{
		if (motion == Vector2.Zero)
			return;

		if (!ignoreEnemyCollisions)
		{
			if (!TestMove(GlobalTransform, motion))
			{
				GlobalPosition += motion;
				return;
			}

			MoveAndCollide(motion);
			return;
		}

		Vector2 remaining = motion;
		int iterations = 0;
		while (remaining.LengthSquared() > 0.0001f && iterations++ < 8)
		{
			if (!TestMove(GlobalTransform, remaining))
			{
				GlobalPosition += remaining;
				return;
			}

			KinematicCollision2D collision = MoveAndCollide(remaining);
			if (collision == null)
				return;

			// Continue moving through other enemies, but stop on solid geometry.
			if (collision.GetCollider() is BasicEnemyController)
			{
				Vector2 traveled = collision.GetTravel();
				if (traveled.LengthSquared() < 0.0001f)
					return;

				Vector2 newRemaining = collision.GetRemainder();
				if (newRemaining.LengthSquared() < 0.0001f)
					return;

				remaining = newRemaining;
				continue;
			}

			return;
		}
	}
}
