﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SynthEBD;

public class VM_AttributeGroupCheckList : VM
{
    public VM_AttributeGroupCheckList(ObservableCollection<VM_AttributeGroup> AttributeVMs)
    {
        foreach (var avm in AttributeVMs)
        {
            this.AttributeSelections.Add(new AttributeSelection(avm, this));
        }

        this.SubscribedMasterList = AttributeVMs;
        this.SubscribedMasterList.CollectionChanged += RefreshCheckList;
    }

    public ObservableCollection<VM_AttributeGroup> SubscribedMasterList { get; set; } // to fire the CollectionChanged event
    public ObservableCollection<AttributeSelection> AttributeSelections { get; set; } = new();

    void RefreshCheckList(object sender, NotifyCollectionChangedEventArgs e)
    {
        var updatedMasterList = (ObservableCollection<VM_AttributeGroup>)sender;
        var newCheckList = new VM_AttributeGroupCheckList(updatedMasterList);
        this.AttributeSelections = newCheckList.AttributeSelections;
    }

    public class AttributeSelection : VM
    {
        public AttributeSelection(VM_AttributeGroup attributeGroupVM, VM_AttributeGroupCheckList parent)
        {
            this.Label = attributeGroupVM.Label;
            this.SubscribedMasterAttributeGroup = attributeGroupVM;
            this.SubscribedMasterAttributeGroup.PropertyChanged += RefreshName;

            this.ParentCheckList = parent;
        }
        public bool IsSelected { get; set; } = false;

        public VM_AttributeGroup SubscribedMasterAttributeGroup { get; set; } // to fire the PropertyChanged event
        public void RefreshName(object sender, PropertyChangedEventArgs e)
        {
            VM_AttributeGroup updatedMasterAttributeGroup = (VM_AttributeGroup)sender;
            this.Label = updatedMasterAttributeGroup.Label;
        }

        public VM_AttributeGroupCheckList ParentCheckList { get; set; }
        public string Label { get; set; }
    }
}