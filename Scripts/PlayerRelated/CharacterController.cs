using Godot;

public partial class CharacterController : CharacterBody2D
{
	[Export] public float Speed = 600f;

	public override void _PhysicsProcess(double delta)
	{
		Vector2 dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (dir.LengthSquared() > 1f)
			dir = dir.Normalized();

		Velocity = dir * Speed;
		MoveAndSlide();
	}
}
