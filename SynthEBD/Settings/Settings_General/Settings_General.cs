﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;

namespace SynthEBD
{
    public class Settings_General
    {
        public Settings_General()
        {
            this.bShowToolTips = true;
            this.bChangeMeshesOrTextures = true;
            this.BodySelectionMode = BodyShapeSelectionMode.None;
            this.BSSelectionMode = BodySlideSelectionMode.OBody;
            this.bChangeHeight = false;
            this.OutputDataFolder = GameEnvironmentProvider.MyEnvironment.DataFolderPath;
            this.bEnableConsistency = true;
            this.ExcludePlayerCharacter = true;
            this.ExcludePresets = true;
            this.bLinkNPCsWithSameName = true;
            this.patchFileName = "SynthEBD";
            this.bVerboseModeAssetsNoncompliant = false;
            this.bVerboseModeAssetsAll = false;
            this.verboseModeNPClist = new List<FormKey>();
            this.bLoadSettingsFromDataFolder = false;
            this.patchableRaces = new List<FormKey>();
            this.raceAliases = new List<RaceAlias>();
            this.RaceGroupings = new List<RaceGrouping>();
            this.AttributeGroups = new HashSet<AttributeGroup>();
            this.OverwritePluginAttGroups = true;
        }

        public bool bShowToolTips { get; set; }
        public bool bChangeMeshesOrTextures { get; set; }
        public BodyShapeSelectionMode BodySelectionMode { get; set; }
        public BodySlideSelectionMode BSSelectionMode { get; set; }
        public bool bChangeHeight { get; set; }
        public string OutputDataFolder { get; set; }
        public bool bEnableConsistency { get; set; }
        public bool ExcludePlayerCharacter { get; set; }
        public bool ExcludePresets { get; set; }
        public bool bLinkNPCsWithSameName { get; set; }
        public string patchFileName { get; set; }

        public bool bVerboseModeAssetsNoncompliant { get; set; }
        public bool bVerboseModeAssetsAll { get; set; }
        public List<FormKey> verboseModeNPClist { get; set; } // enable FormKey (multi?) picker for this
        public bool bLoadSettingsFromDataFolder { get; set; }

        public List<FormKey> patchableRaces { get; set; } // enable FormKey (multi?) picker for this

        public List<RaceAlias> raceAliases { get; set; }

        public List<RaceGrouping> RaceGroupings { get; set; }
        public HashSet<AttributeGroup> AttributeGroups { get; set; }
        public bool OverwritePluginAttGroups { get; set; }
    }

    public enum BodyShapeSelectionMode
    {
        None,
        BodyGen,
        BodySlide
    }

    public enum BodySlideSelectionMode
    {
        OBody,
        AutoBody
    }
}