using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class PlayerHurtbox : Area2D
{
    // -1 uses the enemy's configured KnockbackDistance, 0 disables knockback entirely
    [Export(PropertyHint.Range, "-1,500,1")] public float EnemyKnockbackDistance = -1f;
    // When set to 0, defaults to the actual knockback distance of the hit enemy
    [Export(PropertyHint.Range, "0,2000,1")] public float KnockbackChainLength = 0f;
    // When set to 0, defaults to the same distance, creating a circular reach zone
    [Export(PropertyHint.Range, "0,2000,1")] public float KnockbackChainRadius = 0f;
    // Includes the primary enemy; 0 = unlimited
    [Export(PropertyHint.Range, "0,64,1")] public int KnockbackChainMaxTargets = 0;
    // Scales the knockback distance for neighboring enemies (0 = no splash, 1 = same as main target)
    [Export(PropertyHint.Range, "0,1,0.01")] public float NeighborKnockbackMultiplier = 0.7f;
    // Lower bound so clustered enemies still move a meaningful distance
    [Export(PropertyHint.Range, "0,1,0.01")] public float NeighborKnockbackMinFraction = 0.4f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float DamageFlashDuration = 0.2f;
    [Export] public Color DamageFlashColor = new Color(0.9f, 0.1f, 0.1f, 1f);

    private Health _health;
    private readonly List<KnockbackCandidate> _knockbackCandidates = new();
    private ColorRect _damageFlashRect;
    private Color _damageFlashBaseColor;
    private double _damageFlashTimer;

    private struct KnockbackCandidate
    {
        public KnockbackCandidate(BasicEnemyController enemy, float forward, float lateral)
        {
            Enemy = enemy;
            ForwardDistance = forward;
            LateralDistance = lateral;
        }

        public BasicEnemyController Enemy { get; }
        public float ForwardDistance { get; }
        public float LateralDistance { get; }
    }

    public override void _Ready()
    {
        _health = GetParent().GetNodeOrNull<Health>("Health");
        if (_health == null)
        {
            GD.PrintErr("PlayerHurtbox: Could not find Health node at Player/Health.");
            return;
        }

        AreaEntered += OnAreaEntered;

        var parentColorRect = GetParent()?.GetNodeOrNull<ColorRect>("Player");
        if (parentColorRect != null)
        {
            _damageFlashRect = parentColorRect;
            _damageFlashBaseColor = _damageFlashRect.Color;
        }
    }

    public override void _Process(double delta)
    {
        if (_damageFlashRect == null || _damageFlashTimer <= 0.0)
            return;

        _damageFlashTimer -= delta;
        if (_damageFlashTimer <= 0.0)
            _damageFlashRect.Color = _damageFlashBaseColor;
    }

    private void OnAreaEntered(Area2D area)
    {
        // Generic: any Area2D that implements IDamageSource can damage the player
        if (area is IDamageSource dmg)
        {
            _health.TakeDamage(dmg.Damage);
            KnockbackEnemy(area);
            TriggerDamageFlash();
        }
    }

    private void KnockbackEnemy(Area2D area)
    {
        Node parent = GetParent();
        if (parent is not Node2D playerBody)
            return;

        BasicEnemyController enemy = area.GetParent() as BasicEnemyController
            ?? area.Owner as BasicEnemyController;
        if (enemy == null)
            return;

        Vector2 dir = enemy.GlobalPosition - playerBody.GlobalPosition;
        if (dir.LengthSquared() < 0.0001f)
            return;

        if (EnemyKnockbackDistance == 0f)
            return;

        float distance = EnemyKnockbackDistance > 0f ? EnemyKnockbackDistance : enemy.KnockbackDistance;
        if (distance <= 0f)
            return;

        float duration = enemy.KnockbackDuration;
        ApplyGroupKnockback(enemy, dir, distance, duration);
    }

    private void TriggerDamageFlash()
    {
        if (_damageFlashRect == null)
            return;

        if (DamageFlashDuration <= 0f)
            return;

        _damageFlashTimer = DamageFlashDuration;
        _damageFlashRect.Color = DamageFlashColor;
    }

    private void ApplyGroupKnockback(BasicEnemyController primary, Vector2 direction, float distance, float duration)
    {
        primary.ApplyKnockback(direction, distance, duration);

        float scanLength = KnockbackChainLength > 0f ? KnockbackChainLength : distance;
        float scanRadius = KnockbackChainRadius > 0f ? KnockbackChainRadius : distance;
        if (scanLength <= 0f || scanRadius <= 0f)
            return;

        Vector2 dirNorm = direction.LengthSquared() > 0.0001f ? direction.Normalized() : Vector2.Zero;
        if (dirNorm == Vector2.Zero)
            return;

        SceneTree tree = GetTree();
        if (tree == null)
            return;

        Array<Node> nodes = tree.GetNodesInGroup(BasicEnemyController.EnemyGroupName);
        if (nodes == null || nodes.Count == 0)
            return;

        _knockbackCandidates.Clear();

        foreach (Node node in nodes)
        {
            if (node == primary || node is not BasicEnemyController other)
                continue;

            Vector2 offset = other.GlobalPosition - primary.GlobalPosition;
            float forward = offset.Dot(dirNorm);
            if (forward <= 0f || forward > scanLength)
                continue;

            Vector2 lateral = offset - dirNorm * forward;
            float lateralLength = lateral.Length();
            if (lateralLength > scanRadius)
                continue;

            _knockbackCandidates.Add(new KnockbackCandidate(other, forward, lateralLength));
        }

        if (_knockbackCandidates.Count == 0)
            return;

        _knockbackCandidates.Sort((a, b) => a.ForwardDistance.CompareTo(b.ForwardDistance));

        int applied = 1; // primary already handled
        foreach (KnockbackCandidate candidate in _knockbackCandidates)
        {
            if (KnockbackChainMaxTargets > 0 && applied >= KnockbackChainMaxTargets)
                break;

            float falloffForward = 1f - Mathf.Clamp(candidate.ForwardDistance / scanLength, 0f, 1f);
            float falloffLateral = 1f - Mathf.Clamp(candidate.LateralDistance / scanRadius, 0f, 1f);
            float falloff = Mathf.Clamp(falloffForward * falloffLateral, 0f, 1f);
            float scaledFraction = NeighborKnockbackMultiplier * falloff;
            scaledFraction = Mathf.Max(NeighborKnockbackMinFraction, scaledFraction);
            float scaledDistance = distance * Mathf.Clamp(scaledFraction, 0f, 1f);
            if (scaledDistance <= 0.01f)
                continue;

            candidate.Enemy.ApplyKnockback(direction, scaledDistance, duration);
            applied++;
        }
    }
}
