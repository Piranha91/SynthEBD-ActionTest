﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthEBD
{
    class VM_SpecificNPCAssignmentsUI : INotifyPropertyChanged
    {
        public VM_SpecificNPCAssignmentsUI(VM_SettingsTexMesh texMeshSettings, VM_SettingsBodyGen bodyGenSettings)
        {
            this.Assignments = new ObservableCollection<VM_SpecificNPCAssignment>();
            this.BodyGenSettings = bodyGenSettings;
            this.TexMeshSettings = texMeshSettings;

            AddAssignment = new SynthEBD.RelayCommand(
                canExecute: _ => true,
                execute: _ => this.Assignments.Add(new VM_SpecificNPCAssignment(texMeshSettings.AssetPacks, bodyGenSettings))
                );

            RemoveAssignment = new SynthEBD.RelayCommand(
                canExecute: _ => true,
                execute: x => this.Assignments.Remove((VM_SpecificNPCAssignment)x)
                );

            ImportFromZEBDcommand = new SynthEBD.RelayCommand(
                canExecute: _ => true,
                execute: _ => ImportFromZEBD()
                );
        }

        public ObservableCollection<VM_SpecificNPCAssignment> Assignments { get; set; }
        public VM_SpecificNPCAssignment CurrentlyDisplayedAssignment { get; set; }

        public VM_SettingsBodyGen BodyGenSettings { get; set; }

        VM_SettingsTexMesh TexMeshSettings { get; set; }

        public RelayCommand AddAssignment { get; set; }
        public RelayCommand RemoveAssignment { get; set; }

        public RelayCommand ImportFromZEBDcommand { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public static void GetViewModelFromModels(VM_SpecificNPCAssignmentsUI viewModel, HashSet<SpecificNPCAssignment> assignments)
        {
            var env = new GameEnvironmentProvider().MyEnvironment;
            foreach (var assignment in assignments)
            {
                viewModel.Assignments.Add(VM_SpecificNPCAssignment.GetViewModelFromModel(assignment, viewModel.TexMeshSettings.AssetPacks, viewModel.BodyGenSettings, env));
            }
        }

        public void ImportFromZEBD()
        {
            // Configure open file dialog box
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.FileName = "ForceNPCList"; // Default file name
            dialog.DefaultExt = ".json"; // Default file extension
            dialog.Filter = "JSON files (.json|*.json"; // Filter files by extension

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;

                try
                {
                    var zSpecificNPCAssignments = DeserializeFromJSON<HashSet<zEBDSpecificNPCAssignment>>.loadJSONFile(filename);
                    var newModels = zEBDSpecificNPCAssignment.ToSynthEBDNPCAssignments(zSpecificNPCAssignments);

                    var env = new GameEnvironmentProvider().MyEnvironment;

                    foreach (var model in newModels)
                    {
                        var assignmentVM = VM_SpecificNPCAssignment.GetViewModelFromModel(model, this.TexMeshSettings.AssetPacks, this.BodyGenSettings, env);
                        if (assignmentVM != null) // null if the imported NPC doesn't exist in the current load order
                        {
                            this.Assignments.Add(assignmentVM);
                        }
                    }
                }
                catch
                {
                    // Warn user
                }
            }
        }
    }
}