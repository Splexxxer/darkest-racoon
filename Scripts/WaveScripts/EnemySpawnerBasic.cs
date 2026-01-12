using Godot;
using System;

public partial class EnemySpawnerBasic : Node2D
{
	[Export] public PackedScene EnemyScene;

	// Drag the Player node path in the Inspector; resolved at runtime.
	[Export] public NodePath PlayerPath;

	// Fixed radius: enemies spawn exactly at this distance from the player
	[Export(PropertyHint.Range, "0,5000,1")]
	public float SpawnRadius = 600f;

	// Timing
	[Export(PropertyHint.Range, "0.05,60,0.05")]
	public float CooldownSeconds = 1.5f;

	[Export(PropertyHint.Range, "0,60,0.05")]
	public float InitialDelaySeconds = 0.0f;

	// Control
	[Export] public bool SpawningEnabled = true;

	[Export(PropertyHint.Range, "1,50,1")]
	public int SpawnCountPerWave = 1;

	// 0 = unlimited
	[Export(PropertyHint.Range, "0,500,1")]
	public int MaxAlive = 20;

	private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
	private Node2D _player;
	private double _timeUntilNextSpawn;

	public override void _Ready()
	{
		_rng.Randomize();
		_timeUntilNextSpawn = Math.Max(0.0, InitialDelaySeconds);

		if (EnemyScene == null)
			GD.PrintErr("EnemySpawnerBasic: EnemyScene is not set.");

		GD.Print($"EnemySpawnerBasic: _Ready on {GetPath()} with PlayerPath='{PlayerPath}' (empty: {PlayerPath.IsEmpty})");

		if (PlayerPath != null && !PlayerPath.IsEmpty)
		{
			_player = GetNodeOrNull<Node2D>(PlayerPath);
			GD.Print(_player != null
				? $"EnemySpawnerBasic: resolved Player via path '{PlayerPath}' -> {_player.GetPath()}"
				: $"EnemySpawnerBasic: failed to resolve Player via path '{PlayerPath}'");
		}

		// Fallback: try to find any node named "Player" in the scene tree
		if (_player == null)
		{
			var found = GetTree().Root.FindChild("Player", true, false);
			_player = found as Node2D;
			GD.Print(_player != null
				? $"EnemySpawnerBasic: fallback located '{_player.Name}' at {_player.GetPath()}"
				: "EnemySpawnerBasic: fallback search did not find a Node2D named 'Player'");
		}

		if (_player == null)
			GD.PrintErr("EnemySpawnerBasic: Player reference is not set. Assign PlayerPath in the Inspector.");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!SpawningEnabled || EnemyScene == null || _player == null)
			return;

		if (MaxAlive > 0 && GetAliveEnemyCount() >= MaxAlive)
			return;

		_timeUntilNextSpawn -= delta;
		if (_timeUntilNextSpawn > 0.0)
			return;

		for (int i = 0; i < SpawnCountPerWave; i++)
		{
			if (MaxAlive > 0 && GetAliveEnemyCount() >= MaxAlive)
				break;

			SpawnOneOnCircleEdge();
		}

		_timeUntilNextSpawn = CooldownSeconds;
	}

	private void SpawnOneOnCircleEdge()
	{
		// If SpawnRadius == 0, it would spawn on top of the player; allow it only if you want that.
		float angle = _rng.RandfRange(0f, Mathf.Tau);
		Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SpawnRadius;
		Vector2 spawnPos = _player.GlobalPosition + offset;

		// Instantiate as Node2D so this works for any enemy root that is Node2D (CharacterBody2D is fine).
		var enemy = EnemyScene.Instantiate<Node2D>();
		enemy.GlobalPosition = spawnPos;
		AddChild(enemy);

		// Generic: any enemy that supports targeting gets the Player reference
		if (enemy is IPlayerTargeted targeted)
			targeted.Target = _player;
	}

	private int GetAliveEnemyCount()
	{
		// Counts only spawned enemies that are children of the spawner.
		// If you later add helper nodes under the spawner, switch to groups.
		return GetChildCount();
	}
}
