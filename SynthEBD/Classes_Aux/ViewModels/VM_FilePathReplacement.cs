﻿using System.Collections.ObjectModel;
using System.Windows.Media;
using System.IO;
using ReactiveUI;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Cache;

namespace SynthEBD;

public class VM_FilePathReplacement : VM, IImplementsRecordIntellisense
{
    public VM_FilePathReplacement(VM_FilePathReplacementMenu parentMenu)
    {
        ReferenceNPCFormKey = parentMenu.ReferenceNPCFK;
        LinkCache = parentMenu.LinkCache;

        RecordIntellisense.InitializeSubscriptions(this);
        parentMenu.WhenAnyValue(x => x.ReferenceNPCFK).Subscribe(x => SyncReferenceWithParent()); // can be changed from record templates without the user modifying parentMenu.NPCFK, so need an explicit watch
        parentMenu.WhenAnyValue(x => x.LinkCache).Subscribe(x => LinkCache = parentMenu.LinkCache);

        DeleteCommand = new RelayCommand(canExecute: _ => true, execute: _ => parentMenu.Paths.Remove(this));
        FindPath = new SynthEBD.RelayCommand(
            canExecute: _ => true,
            execute: _ =>
            {
                System.Windows.Forms.OpenFileDialog dialog = LongPathHandler.CreateLongPathOpenFileDialog();
                if (Source != "")
                {
                    var initDir = Path.Combine(PatcherEnvironmentProvider.Instance.Environment.DataFolderPath, Path.GetDirectoryName(Source));
                    if (Directory.Exists(initDir))
                    {
                        dialog.InitialDirectory = initDir;
                    }
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // try to figure out the root directory
                    if (dialog.FileName.Contains(PatcherEnvironmentProvider.Instance.Environment.DataFolderPath))
                    {
                        Source = dialog.FileName.Replace(PatcherEnvironmentProvider.Instance.Environment.DataFolderPath, "").TrimStart(Path.DirectorySeparatorChar);
                    }
                    else if (TrimKnownPrefix(dialog.FileName, out var sourceTrimmed))
                    {
                        Source = sourceTrimmed;
                    }
                    else if (dialog.FileName.Contains("Data", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var index = dialog.FileName.IndexOf("Data", 0, StringComparison.InvariantCultureIgnoreCase);
                        Source = dialog.FileName.Remove(0, index + 4).TrimStart(Path.DirectorySeparatorChar);
                    }
                    else
                    {
                        CustomMessageBox.DisplayNotificationOK("Parsing Error", "Cannot figure out where the Data folder is within the supplied path. You will need to edit the path so that it starts one folder beneath the Data folder.");
                        Source = dialog.FileName;
                    }
                }
            }
        );


        ParentMenu = parentMenu;

        this.WhenAnyValue(x => x.Source).Subscribe(x => RefreshSourceColor());
        this.WhenAnyValue(x => x.IntellisensedPath).Subscribe(x => RefreshDestColor());
        this.WhenAnyValue(x => x.ReferenceNPCFormKey).Subscribe(x => RefreshDestColor());
    }

    public VM_FilePathReplacement Clone(VM_FilePathReplacementMenu parentMenu)
    {
        VM_FilePathReplacement clone = new VM_FilePathReplacement(parentMenu);
        clone.Source = Source;
        clone.IntellisensedPath = this.IntellisensedPath;
        clone.ReferenceNPCFormKey = this.ReferenceNPCFormKey.DeepCopyByExpressionTree();
        return clone;
    }

    public string Source { get; set; } = "";
    public string IntellisensedPath { get; set; } = "";

    public SolidColorBrush SourceBorderColor { get; set; } = new(Colors.Red);
    public SolidColorBrush DestBorderColor { get; set; } = new(Colors.Red);

    public RelayCommand DeleteCommand { get; }
    public RelayCommand FindPath { get; }

    public VM_FilePathReplacementMenu ParentMenu { get; set; }
    public RecordIntellisense.PathSuggestion ChosenPathSuggestion { get; set; } = new();
    public ObservableCollection<RecordIntellisense.PathSuggestion> PathSuggestions { get; set; } = new();
    public FormKey ReferenceNPCFormKey { get; set; }
    public ILinkCache LinkCache { get; set; }

    public static VM_FilePathReplacement GetViewModelFromModel(FilePathReplacement model, VM_FilePathReplacementMenu parentMenu)
    {
        VM_FilePathReplacement viewModel = new VM_FilePathReplacement(parentMenu);
        viewModel.Source = model.Source;
        viewModel.IntellisensedPath = model.Destination;

        return viewModel;
    }

    public void RefreshSourceColor()
    {
        var searchStr = Path.Combine(PatcherEnvironmentProvider.Instance.Environment.DataFolderPath, this.Source);
        if (LongPathHandler.PathExists(searchStr) || BSAHandler.ReferencedPathExists(this.Source, out _, out _))
        {
            this.SourceBorderColor = new SolidColorBrush(Colors.LightGreen);
        }
        else
        {
            this.SourceBorderColor = new SolidColorBrush(Colors.Red);
        }
    }

    public void RefreshDestColor()
    {
        if(LinkCache != null && ReferenceNPCFormKey != null && LinkCache.TryResolve<INpcGetter>(ReferenceNPCFormKey, out var refNPC) && RecordPathParser.GetObjectAtPath(refNPC, this.IntellisensedPath, new Dictionary<string, dynamic>(), ParentMenu.LinkCache, true, Logger.GetNPCLogNameString(refNPC), out var objAtPath) && objAtPath is not null && objAtPath.GetType() == typeof(string))
        {
            this.DestBorderColor = new SolidColorBrush(Colors.LightGreen);
        }
        else
        {
            this.DestBorderColor = new SolidColorBrush(Colors.Red);
        }
    }

    private static bool TrimKnownPrefix(string s, out string trimmed)
    {
        trimmed = "";
        foreach (var trim in PatcherSettings.TexMesh.TrimPaths)
        {
            if (s.Contains(trim.PathToTrim) && s.EndsWith(trim.Extension))
            {
                trimmed = s.Remove(0, s.IndexOf(trim.PathToTrim, StringComparison.OrdinalIgnoreCase)).TrimStart(Path.DirectorySeparatorChar);
                return true;
            }
        }
        return false;
    }

    private void SyncReferenceWithParent()
    {
        if (ParentMenu != null)
        {
            this.ReferenceNPCFormKey = ParentMenu.ReferenceNPCFK;
        }
    }
}