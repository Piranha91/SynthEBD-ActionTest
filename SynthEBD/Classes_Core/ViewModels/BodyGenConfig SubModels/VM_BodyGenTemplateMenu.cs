﻿using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using ReactiveUI;

namespace SynthEBD;

public class VM_BodyGenTemplateMenu : VM
{
    public VM_BodyGenTemplateMenu(VM_BodyGenConfig parentConfig, ObservableCollection<VM_RaceGrouping> raceGroupingVMs)
    {
        this.CurrentlyDisplayedTemplate = new VM_BodyGenTemplate(parentConfig.GroupUI.TemplateGroups, parentConfig.DescriptorUI, raceGroupingVMs, this.Templates, parentConfig);
        AddTemplate = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.Templates.Add(new VM_BodyGenTemplate(parentConfig.GroupUI.TemplateGroups, parentConfig.DescriptorUI, raceGroupingVMs, this.Templates, parentConfig))
        );

        RemoveTemplate = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: x => this.Templates.Remove((VM_BodyGenTemplate)x)
        );
    }
    public ObservableCollection<VM_BodyGenTemplate> Templates { get; set; } = new();

    public VM_BodyGenTemplate CurrentlyDisplayedTemplate { get; set; }

    public RelayCommand AddTemplate { get; }
    public RelayCommand RemoveTemplate { get; }
}


public class VM_BodyGenTemplate : VM
{
    public VM_BodyGenTemplate(ObservableCollection<VM_CollectionMemberString> templateGroups, VM_BodyShapeDescriptorCreationMenu BodyShapeDescriptors, ObservableCollection<VM_RaceGrouping> raceGroupingVMs, ObservableCollection<VM_BodyGenTemplate> parentCollection, VM_BodyGenConfig parentConfig)
    {
        this.GroupSelectionCheckList = new VM_CollectionMemberStringCheckboxList(templateGroups);
        this.DescriptorsSelectionMenu = new VM_BodyShapeDescriptorSelectionMenu(BodyShapeDescriptors, raceGroupingVMs, parentConfig);
        this.AllowedRaceGroupings = new VM_RaceGroupingCheckboxList(raceGroupingVMs);
        this.DisallowedRaceGroupings = new VM_RaceGroupingCheckboxList(raceGroupingVMs);

        this.ParentConfig = parentConfig;
        this.ParentCollection = parentCollection;
        parentCollection.CollectionChanged += UpdateOtherGroupsTemplateCollection;
        templateGroups.CollectionChanged += UpdateOtherGroupsTemplateCollection;
        this.GroupSelectionCheckList.PropertyChanged += UpdateOtherGroupsTemplateCollectionP;
        
        PatcherEnvironmentProvider.Instance.WhenAnyValue(x => x.Environment.LinkCache)
            .Subscribe(x => lk = x)
            .DisposeWith(this);

        AddAllowedAttribute = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.AllowedAttributes.Add(VM_NPCAttribute.CreateNewFromUI(this.AllowedAttributes, true, null, ParentConfig.AttributeGroupMenu.Groups))
        );

        AddDisallowedAttribute = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.DisallowedAttributes.Add(VM_NPCAttribute.CreateNewFromUI(this.DisallowedAttributes, false, null, ParentConfig.AttributeGroupMenu.Groups))
        );

        AddRequiredTemplate = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.RequiredTemplates.Add(new VM_CollectionMemberString("", this.RequiredTemplates))
        );

        DeleteMe = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ => this.ParentCollection.Remove(this)
        );
    }

    public string Label { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Specs { get; set; } = ""; // will need special logic during I/O because in zEBD settings this is called "params" which is reserved in C#
    public VM_CollectionMemberStringCheckboxList GroupSelectionCheckList { get; set; }
    public VM_BodyShapeDescriptorSelectionMenu DescriptorsSelectionMenu { get; set; }
    public ObservableCollection<FormKey> AllowedRaces { get; set; } = new();
    public ObservableCollection<FormKey> DisallowedRaces { get; set; } = new();
    public VM_RaceGroupingCheckboxList AllowedRaceGroupings { get; set; }
    public VM_RaceGroupingCheckboxList DisallowedRaceGroupings { get; set; }
    public ObservableCollection<VM_NPCAttribute> AllowedAttributes { get; set; } = new(); // keeping as array to allow deserialization of original zEBD settings files
    public ObservableCollection<VM_NPCAttribute> DisallowedAttributes { get; set; } = new();
    public bool bAllowUnique { get; set; } = true;
    public bool bAllowNonUnique { get; set; } = true;
    public bool bAllowRandom { get; set; } = true;
    public double ProbabilityWeighting { get; set; } = 1;
    public ObservableCollection<VM_CollectionMemberString> RequiredTemplates { get; set; } = new();
    public NPCWeightRange WeightRange { get; set; } = new();
    public string Caption_MemberOfTemplateGroups { get; set; } = "";
    public string Caption_BodyShapeDescriptors { get; set; } = "";

    public ILinkCache lk { get; private set; }
    public IEnumerable<Type> RacePickerFormKeys { get; set; } = typeof(IRaceGetter).AsEnumerable();

    public RelayCommand AddAllowedAttribute { get; }
    public RelayCommand AddDisallowedAttribute { get; }
    public RelayCommand AddRequiredTemplate { get; }
    public RelayCommand DeleteMe { get; }

    public VM_BodyGenConfig ParentConfig { get; set; }
    public ObservableCollection<VM_BodyGenTemplate> ParentCollection {get; set;}
    public ObservableCollection<VM_BodyGenTemplate> OtherGroupsTemplateCollection { get; set; } = new();

    public static void GetViewModelFromModel(BodyGenConfig.BodyGenTemplate model, VM_BodyGenTemplate viewModel, VM_BodyShapeDescriptorCreationMenu descriptorMenu, ObservableCollection<VM_RaceGrouping> raceGroupingVMs)
    {
        viewModel.Label = model.Label;
        viewModel.Notes = model.Notes;
        viewModel.Specs = model.Specs;
        viewModel.GroupSelectionCheckList.InitializeFromHashSet(model.MemberOfTemplateGroups);
        viewModel.DescriptorsSelectionMenu = VM_BodyShapeDescriptorSelectionMenu.InitializeFromHashSet(model.BodyShapeDescriptors, descriptorMenu, raceGroupingVMs, viewModel.ParentConfig);
        viewModel.AllowedRaces = new ObservableCollection<FormKey>(model.AllowedRaces);
        viewModel.AllowedRaceGroupings = new VM_RaceGroupingCheckboxList(raceGroupingVMs);
        foreach (var grouping in viewModel.AllowedRaceGroupings.RaceGroupingSelections)
        {
            if (model.AllowedRaceGroupings.Contains(grouping.Label))
            {
                grouping.IsSelected = true;
            }
            else { grouping.IsSelected = false; }
        }

        viewModel.DisallowedRaces = new ObservableCollection<FormKey>(model.DisallowedRaces);
        viewModel.DisallowedRaceGroupings = new VM_RaceGroupingCheckboxList(raceGroupingVMs);
            
        foreach (var grouping in viewModel.DisallowedRaceGroupings.RaceGroupingSelections)
        {
            if (model.DisallowedRaceGroupings.Contains(grouping.Label))
            {
                grouping.IsSelected = true;
            }
            else { grouping.IsSelected = false; }
        }

        viewModel.AllowedAttributes = VM_NPCAttribute.GetViewModelsFromModels(model.AllowedAttributes, viewModel.ParentConfig.AttributeGroupMenu.Groups, true, null);
        viewModel.DisallowedAttributes = VM_NPCAttribute.GetViewModelsFromModels(model.DisallowedAttributes, viewModel.ParentConfig.AttributeGroupMenu.Groups, false, null);
        foreach (var x in viewModel.DisallowedAttributes) { x.DisplayForceIfOption = false; }
        viewModel.bAllowUnique = model.AllowUnique;
        viewModel.bAllowNonUnique = model.AllowNonUnique;
        viewModel.bAllowRandom = model.AllowRandom;
        viewModel.ProbabilityWeighting = model.ProbabilityWeighting;
        viewModel.RequiredTemplates = VM_CollectionMemberString.InitializeCollectionFromHashSet(model.RequiredTemplates);
        viewModel.WeightRange = model.WeightRange;
    }

    public static BodyGenConfig.BodyGenTemplate DumpViewModelToModel(VM_BodyGenTemplate viewModel)
    {
        BodyGenConfig.BodyGenTemplate model = new BodyGenConfig.BodyGenTemplate();
        model.Label = viewModel.Label;
        model.Notes = viewModel.Notes;
        model.Specs = viewModel.Specs;
        model.MemberOfTemplateGroups = viewModel.GroupSelectionCheckList.CollectionMemberStrings.Where(x => x.IsSelected).Select(x => x.SubscribedString.Content).ToHashSet();
        model.BodyShapeDescriptors = VM_BodyShapeDescriptorSelectionMenu.DumpToHashSet(viewModel.DescriptorsSelectionMenu);
        model.AllowedRaces = viewModel.AllowedRaces.ToHashSet();
        model.AllowedRaceGroupings = viewModel.AllowedRaceGroupings.RaceGroupingSelections.Where(x => x.IsSelected).Select(x => x.Label).ToHashSet();
        model.DisallowedRaces = viewModel.DisallowedRaces.ToHashSet();
        model.DisallowedRaceGroupings = viewModel.DisallowedRaceGroupings.RaceGroupingSelections.Where(x => x.IsSelected).Select(x => x.Label).ToHashSet();
        model.AllowedAttributes = VM_NPCAttribute.DumpViewModelsToModels(viewModel.AllowedAttributes);
        model.DisallowedAttributes = VM_NPCAttribute.DumpViewModelsToModels(viewModel.DisallowedAttributes);
        model.AllowUnique = viewModel.bAllowUnique;
        model.AllowNonUnique = viewModel.bAllowNonUnique;
        model.AllowRandom = viewModel.bAllowRandom;
        model.ProbabilityWeighting = viewModel.ProbabilityWeighting;
        model.RequiredTemplates = viewModel.RequiredTemplates.Select(x => x.Content).ToHashSet();
        model.WeightRange = viewModel.WeightRange;
        return model;
    }

    public void UpdateOtherGroupsTemplateCollection(object sender, NotifyCollectionChangedEventArgs e)
    {
        var excludedCollection = this.UpdateThisOtherGroupsTemplateCollection();
        foreach(var template in excludedCollection)
        {
            template.UpdateThisOtherGroupsTemplateCollection();
        }
    }

    public void UpdateOtherGroupsTemplateCollectionP(object sender, PropertyChangedEventArgs e)
    {
        var excludedCollection = this.UpdateThisOtherGroupsTemplateCollection();
        foreach (var template in excludedCollection)
        {
            template.UpdateThisOtherGroupsTemplateCollection();
        }
    }

    public ObservableCollection<VM_BodyGenTemplate> UpdateThisOtherGroupsTemplateCollection()
    {
        var updatedCollection = new ObservableCollection<VM_BodyGenTemplate>();
        var excludedCollection = new ObservableCollection<VM_BodyGenTemplate>();

        foreach (var template in this.ParentCollection)
        {
            bool inGroup = false;
            foreach (var group in template.GroupSelectionCheckList.CollectionMemberStrings)
            {
                if (group.IsSelected == false) { continue; }

                foreach (var thisGroup in this.GroupSelectionCheckList.CollectionMemberStrings)
                {
                    if (thisGroup.IsSelected == false) { continue; }
                        
                    if (group.SubscribedString == thisGroup.SubscribedString)
                    {
                        inGroup = true;
                        break;
                    }
                }
                if (inGroup == true) { break; }
            }

            if (inGroup == false)
            {
                updatedCollection.Add(template);
            }
            else
            {
                excludedCollection.Add(template);
            }    
        }

        this.OtherGroupsTemplateCollection = updatedCollection;

        return excludedCollection;
    }
}