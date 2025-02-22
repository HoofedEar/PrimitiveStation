﻿using System.Threading;
using Content.Shared.Sound;

namespace Content.Server.Prim14.Bush;

[RegisterComponent]
[Access(typeof(BushSystem))]
public sealed class BushComponent : Component
{
    /// <summary>
    /// Entity that is spawned when a player picks from the bush
    /// </summary>
    [ViewVariables]
    [DataField("loot")]
    public string? Loot;

    /// <summary>
    /// How many should be spawned?
    /// </summary>
    [ViewVariables]
    [DataField("quantity")]
    public int? Quantity = 1;

    /// <summary>
    /// Time it takes to respawn the loot
    /// </summary>
    [ViewVariables]
    [DataField("respawnTime")]
    public float? RespawnTime = 30f;

    /// <summary>
    /// The sound played when interacting with the bush
    /// </summary>
    [ViewVariables]
    [DataField("interactSound")]
    public SoundSpecifier? InteractSound;

    public float Accumulator;
    public bool Ready = true;
    public CancellationTokenSource? CancelToken;
}
