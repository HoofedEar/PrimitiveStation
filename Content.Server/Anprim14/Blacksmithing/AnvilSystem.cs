﻿using Content.Server.Anprim14.Blacksmithing.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;

namespace Content.Server.Anprim14.Blacksmithing;

public sealed class AnvilSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnvilComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<AnvilComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<AnvilComponent, ExaminedEvent>(OnExamined);
    }

    private void OnComponentInit(EntityUid uid, AnvilComponent jug, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, AnvilComponent.AnvilMoldSlotId, jug.MoldSlot);
    }

    private void OnComponentRemove(EntityUid uid, AnvilComponent jug, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, jug.MoldSlot);
    }

    private void OnExamined(EntityUid uid, AnvilComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!TryComp(component.MoldSlot.Item, out SolutionContainerManagerComponent? solutionComp))
        {
            return;
        }

        if (solutionComp.Solutions.Count == 0)
        {
            
            return;
        }

        foreach (var (name, solution) in solutionComp.Solutions)
        {
            if (name != "metal")
                return;

            if (solution.CurrentVolume != 20)
            {
                args.Message.AddText("\nIt has an empty mold.");
                break;
            }
            
            args.Message.AddText("\nIt has a mold ready to smith.");
            break;
        }
    }
}