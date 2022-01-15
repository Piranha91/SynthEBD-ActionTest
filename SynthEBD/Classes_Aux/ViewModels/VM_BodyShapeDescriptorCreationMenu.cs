﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthEBD
{
    public class VM_BodyShapeDescriptorCreationMenu : INotifyPropertyChanged
    {
        public VM_BodyShapeDescriptorCreationMenu()
        {
            this.TemplateDescriptors = new ObservableCollection<VM_BodyShapeDescriptorShell>();
            this.TemplateDescriptorList = new ObservableCollection<VM_BodyShapeDescriptor>();
            this.CurrentlyDisplayedTemplateDescriptorShell = new VM_BodyShapeDescriptorShell(new ObservableCollection<VM_BodyShapeDescriptorShell>());

            AddTemplateDescriptorShell = new SynthEBD.RelayCommand(
                canExecute: _ => true,
                execute: _ => this.TemplateDescriptors.Add(new VM_BodyShapeDescriptorShell(this.TemplateDescriptors))
                );

            RemoveTemplateDescriptorShell = new SynthEBD.RelayCommand(
                canExecute: _ => true,
                execute: x => this.TemplateDescriptors.Remove((VM_BodyShapeDescriptorShell)x)
                );
        }

        public ObservableCollection<VM_BodyShapeDescriptorShell> TemplateDescriptors { get; set; }
        public ObservableCollection<VM_BodyShapeDescriptor> TemplateDescriptorList { get; set; } // hidden flattened list of TemplateDescriptors for presentation to VM_Subgroup and VM_BodyGenTemplate. Needs to be synced with TemplateDescriptors on update.

        public VM_BodyShapeDescriptorShell CurrentlyDisplayedTemplateDescriptorShell { get; set; }

        public RelayCommand AddTemplateDescriptorShell { get; }
        public RelayCommand RemoveTemplateDescriptorShell { get; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}