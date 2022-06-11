﻿using System.Threading;
using Content.Server.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Random;

namespace Content.Server.Prim14.UseWith;

public sealed class UseWithSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IRobustRandom _random = null!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UseWithComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<UseWithComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<UseWithComponent, UseWithEvent>(TryUseWith);
        SubscribeLocalEvent<UseWithComponent, UseWithCancel>(OnUseWithCancel);
    }

    private void OnInteractUsing(EntityUid uid, UseWithComponent component, InteractUsingEvent args)
    {
        if (component.UseWithWhitelist?.IsValid(args.Used) == false) return;
        
        if (args.Handled && !component.UseInHand)
            return;
        
        OnUseWith(uid, component, args);
        
        args.Handled = true;
    }

    private void OnUseInHand(EntityUid uid, UseWithComponent component, UseInHandEvent args)
    {
        if (args.Handled && !component.UseInHand)
            return;
        
        OnUseWith(uid, component, args);
        
        args.Handled = true;
    }

    private void OnUseWith(EntityUid uid, UseWithComponent component, UseInHandEvent args)
    {
        if (component.CancelToken != null) return;
        
        component.CancelToken = new CancellationTokenSource();
        
        var doAfterEventArgs = new DoAfterEventArgs(args.User, 1, component.CancelToken.Token, uid)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            NeedHand = true,
            TargetFinishedEvent = new UseWithEvent(args.User, true),
            TargetCancelledEvent = new UseWithCancel()
        };
        
        _doAfterSystem.DoAfter(doAfterEventArgs);
    }

    private void OnUseWith(EntityUid uid, UseWithComponent component, InteractUsingEvent args)
    {
        if (component.CancelToken != null) return;
        
        component.CancelToken = new CancellationTokenSource();
        
        var doAfterEventArgs = new DoAfterEventArgs(args.User, 1, component.CancelToken.Token, uid)
        {
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnTargetMove = true,
            NeedHand = true,
            TargetFinishedEvent = new UseWithEvent(args.User, false),
            TargetCancelledEvent = new UseWithCancel()
        };
        
        _doAfterSystem.DoAfter(doAfterEventArgs);
    }

    private void TryUseWith(EntityUid uid, UseWithComponent component, UseWithEvent args)
    {
        component.CancelToken = null;
        if (args.Hand)
            DeleteSpawnHand(component, args.User);
        else
        {
            DeleteAndSpawn(component);
        }
    }
    
    private void OnUseWithCancel(EntityUid uid, UseWithComponent component, UseWithCancel args)
    {
        component.CancelToken = null;
    }
    
    private void DeleteAndSpawn(UseWithComponent component)
    {
        var position = EntityManager.GetComponent<TransformComponent>(component.Owner).Coordinates;
            
        for (var i=0; i < component.SpawnCount; i++)
        {
            //var spawnPos = position.Offset(_random.NextVector2(0.2f));
            EntityManager.SpawnEntity(component.Results, position);
        }

        QueueDel(component.Owner);
    }

    private void DeleteSpawnHand(UseWithComponent component, EntityUid user)
    {
        var groundPos = Transform(user).MapPosition;
        EntityUid finisher = default;
        if (component.SpawnCount != 0)
        {
            for (var i=0; i < component.SpawnCount; i++)
            {
                var spawnPos = groundPos.Offset(_random.NextVector2(0.2f));
                finisher = EntityManager.SpawnEntity(component.Results, spawnPos);
            }
        }

        if (!_handsSystem.IsHolding(user, component.Owner, out var hand)) return;
        EntityManager.DeleteEntity(component.Owner);

        // Put it back into their hand
        _handsSystem.TryPickup(user, finisher, hand);
    }
    
    #region DoAfterClasses
    public sealed class UseWithEvent : EntityEventArgs
    {
        public readonly EntityUid User;
        public readonly bool Hand;

        public UseWithEvent(EntityUid user, bool hand)
        {
            User = user;
            Hand = hand;
        }
    }
    
    private sealed class UseWithCancel : EntityEventArgs { }
    
    #endregion
}
