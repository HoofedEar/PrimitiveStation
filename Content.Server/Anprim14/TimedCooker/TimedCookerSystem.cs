﻿using Content.Server.Popups;
using Content.Shared.Interaction;
using Robust.Server.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Anprim14.TimedCooker;

public sealed class TimedCookerSystem : EntitySystem
{
    // TODO later on add checks for regent stuff (spices and such)
    
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;

    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private Queue<EntityUid> _producingAddQueue = new();
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private Queue<EntityUid> _producingRemoveQueue = new();
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<TimedCookerComponent, ComponentInit>(HandleTimedCookerInit);
        SubscribeLocalEvent<TimedCookerComponent, InteractUsingEvent>(OnInteractUsing);
    }
    
    private void HandleTimedCookerInit(EntityUid uid, TimedCookerComponent component, ComponentInit args)
    {
        component.Container = _containerSystem.EnsureContainer<Container>(component.Owner, "cooker_container", out _);
    }
    
    private void OnInteractUsing(EntityUid uid, TimedCookerComponent component, InteractUsingEvent args)
    {
        // Check if the item can be insert, and that it's on the whitelist
        if (!component.Container.CanInsert(args.Used) &&
            component.Whitelist?.IsValid(args.Used) == false)
        {
            _popupSystem.PopupEntity(Loc.GetString("timed-cooker-insert-fail"), uid, Filter.Entities(args.User));
            return;
        }
        
        // Make sure it's not full
        if (component.Queue.Count >= component.Max - 1)
        {
            _popupSystem.PopupEntity(Loc.GetString("timed-cooker-insert-full"), uid, Filter.Entities(args.User));
            return;
        }
        
        // Make sure it has a valid recipe
        if (!TryComp(args.Used, out TimedCookableComponent? cookable))
        { return; }
            
        
        if (cookable.Recipe == null || 
            !_prototypeManager.TryIndex(cookable.Recipe, out TimedCookerRecipePrototype? recipe))
            return;
        
        // Attempt to insert the item
        if (!component.Container.Insert(args.Used))
            return;
        
        // Play the inserting sound (if any)
        if (component.InsertingSound != null)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner, entityManager: EntityManager), component.InsertingSound.GetSound(), component.Owner);
        }
        
        //Queue it up
        if (cookable.Recipe != null)
        {
            component.Queue.Enqueue(recipe);
        }
    }

    public override void Update(float frameTime)
    {
        foreach (var uid in _producingAddQueue)
        {
            EnsureComp<TimedCookerProducingComponent>(uid);
        }

        _producingAddQueue.Clear();
        
        foreach (var uid in _producingRemoveQueue)
        {
            RemComp<TimedCookerProducingComponent>(uid);
        }

        _producingRemoveQueue.Clear();

        foreach (var cooker in EntityQuery<TimedCookerComponent>())
        {
            if (cooker.ProducingRecipe == null)
            {
                if (cooker.Queue.Count > 0)
                {
                    Produce(cooker, cooker.Queue.Dequeue());
                    return;
                }
            }
            if (cooker.ProducingRecipe != null && cooker.ProducingAccumulator < cooker.ProducingRecipe.CompleteTime.TotalSeconds)
            {
                cooker.ProducingAccumulator += frameTime;
                continue;
            }

            cooker.ProducingAccumulator = 0;
            if (cooker.ProducingRecipe != null) FinishProducing(cooker.ProducingRecipe, cooker);
        }
    }

    /// <summary>
    /// If we were able to produce the recipe,
    /// spawn it and cleanup. If we weren't, just do cleanup.
    /// </summary>
    private void FinishProducing(TimedCookerRecipePrototype recipe, TimedCookerComponent component, bool productionSucceeded = true)
    {
        component.ProducingRecipe = null;
        if (productionSucceeded)
        {
            foreach (var result in recipe.Result)
            {
                EntityManager.SpawnEntity(result, Comp<TransformComponent>(component.Owner).Coordinates);
            }
        }
        
        // Play sound
        if (component.ProducingSound != null)
        {
            SoundSystem.Play(Filter.Pvs(component.Owner), component.ProducingSound.GetSound(), component.Owner);
        }
        
        // Continue to next in queue if there are items left
        if (component.Queue.Count > 0)
        {
            Produce(component, component.Queue.Dequeue());
            return;
        }
        _producingRemoveQueue.Enqueue(component.Owner);
        component.Container.CleanContainer();
    }

    /// <summary>
    /// This handles the checks to start producing an item
    /// </summary>
    private void Produce(TimedCookerComponent component, TimedCookerRecipePrototype recipe)
    {
        component.ProducingRecipe = recipe;
        _producingAddQueue.Enqueue(component.Owner);
    }
}