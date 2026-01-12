using Godot;
using System.Collections.Generic;

public partial class CharacterController : CharacterBody2D
{
	[Export] public float Speed = 600f;
	[Export] public PackedScene BulletScene;
	[Export] public float BulletSpeed = 900f;
	[Export] public int BulletDamage = 1;
	[Export(PropertyHint.Range, "0,5,0.01")] public float FireCooldownSeconds = 0.2f;
	public float BulletCooldown
	{
		get => FireCooldownSeconds;
		set => FireCooldownSeconds = Mathf.Max(0f, value);
	}
	[Export] public NodePath BulletSpawnPointPath;
	[Export] public NodePath BulletParentPath;

	private Node2D _bulletSpawnPoint;
	private Node _bulletParent;
	private double _fireCooldownTimer;
	private readonly List<BulletInstance> _activeBullets = new();

	private sealed class BulletInstance
	{
		public Node2D Node;
		public Vector2 Velocity;
		public VisibleOnScreenNotifier2D Notifier;
	}

	public override void _Ready()
	{
		base._Ready();
		if (BulletSpawnPointPath != null && !BulletSpawnPointPath.IsEmpty)
			_bulletSpawnPoint = GetNodeOrNull<Node2D>(BulletSpawnPointPath);

		if (BulletParentPath != null && !BulletParentPath.IsEmpty)
			_bulletParent = GetNodeOrNull(BulletParentPath);

		if (_bulletParent == null)
			_bulletParent = GetTree().CurrentScene ?? GetParent();
	}

	public override void _PhysicsProcess(double delta)
	{
		HandleMovement();
		HandleShooting(delta);
		UpdateBullets(delta);
	}

	private void HandleMovement()
	{
		Vector2 dir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (dir.LengthSquared() > 1f)
			dir = dir.Normalized();

		Velocity = dir * Speed;
		MoveAndSlide();
	}

	private void HandleShooting(double delta)
	{
		if (_fireCooldownTimer > 0.0)
			_fireCooldownTimer = Mathf.Max(0.0, _fireCooldownTimer - delta);

		if (BulletScene == null || BulletSpeed <= 0f)
			return;

		if (!Input.IsMouseButtonPressed(MouseButton.Left))
			return;

		if (_fireCooldownTimer > 0.0)
			return;

		Vector2 target = GetGlobalMousePosition();
		SpawnBulletTowards(target);
	}

	private void SpawnBulletTowards(Vector2 targetGlobal)
	{
		if (BulletScene == null)
			return;

		Node bulletNode = BulletScene.Instantiate();
		if (bulletNode is not Node2D bulletBody)
		{
			GD.PrintErr("CharacterController: BulletScene root must inherit Node2D.");
			bulletNode.QueueFree();
			return;
		}

		Node parent = _bulletParent ?? GetTree().CurrentScene;
		parent?.AddChild(bulletBody);

		Vector2 spawnPos = _bulletSpawnPoint != null ? _bulletSpawnPoint.GlobalPosition : GlobalPosition;
		bulletBody.GlobalPosition = spawnPos;

		Vector2 dir = targetGlobal - spawnPos;
		if (dir.LengthSquared() < 0.0001f)
			dir = Vector2.Right;
		dir = dir.Normalized();

		var instance = new BulletInstance
		{
			Node = bulletBody,
			Velocity = dir * BulletSpeed,
		};

		if (bulletBody is Bullet bulletBehavior)
		{
			bulletBehavior.Shooter = this;
			if (BulletDamage > 0)
				bulletBehavior.Damage = BulletDamage;
		}

		var notifier = new VisibleOnScreenNotifier2D();
		bulletBody.AddChild(notifier);
		instance.Notifier = notifier;
		notifier.ScreenExited += () => OnBulletScreenExited(instance);

		_activeBullets.Add(instance);
		_fireCooldownTimer = FireCooldownSeconds;
	}

	private void UpdateBullets(double delta)
	{
		float step = (float)delta;
		for (int i = _activeBullets.Count - 1; i >= 0; i--)
		{
			BulletInstance bullet = _activeBullets[i];
			if (!GodotObject.IsInstanceValid(bullet.Node))
			{
				_activeBullets.RemoveAt(i);
				continue;
			}

			bullet.Node.GlobalPosition += bullet.Velocity * step;
		}
	}

	private void OnBulletScreenExited(BulletInstance instance)
	{
		RemoveBulletInstance(instance, true);
	}

	public void NotifyBulletRemoved(Node2D bulletNode)
	{
		if (bulletNode == null)
			return;

		for (int i = 0; i < _activeBullets.Count; i++)
		{
			BulletInstance instance = _activeBullets[i];
			if (instance.Node == bulletNode)
			{
				_activeBullets.RemoveAt(i);
				return;
			}
		}
	}

	private void RemoveBulletInstance(BulletInstance instance, bool queueFree)
	{
		if (instance == null)
			return;

		int index = _activeBullets.IndexOf(instance);
		if (index >= 0)
			_activeBullets.RemoveAt(index);

		if (queueFree && GodotObject.IsInstanceValid(instance.Node))
			instance.Node.QueueFree();
	}
}
