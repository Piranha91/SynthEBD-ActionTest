﻿using Newtonsoft.Json;

namespace SynthEBD;

public class Settings_TexMesh
{
    public bool bChangeNPCTextures { get; set; } = true;
    public bool bChangeNPCMeshes { get; set; } = true;
    public bool bApplyToNPCsWithCustomSkins { get; set; } = true;
    public bool bApplyToNPCsWithCustomFaces { get; set; } = true;
    public bool bForceVanillaBodyMeshPath { get; set; } = false;
    public bool bDisplayPopupAlerts { get; set; } = true;
    public bool bGenerateAssignmentLog { get; set; } = true;
    public bool bShowPreviewImages { get; set; } = true;
    public HashSet<string> SelectedAssetPacks { get; set; } = new();

    [JsonIgnore]
    public HashSet<TrimPath> TrimPaths { get; set; } = new();
}