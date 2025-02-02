﻿using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;

/*
 * When adding a new type of ITypedNPCAttribute, don't forget to also include it in JSONhandler.AttributeConverter.ReadJson() so that it can be correctly deserialized
 */

namespace SynthEBD;

// Each NPCAttribute within a HashSet<NPC> Attribute is treated with OR logic; i.e. if an NPC matches ANY of the NPCAttributes, the NPCAttribute's parent object can be assigned to the NPC
public class NPCAttribute
{
    public HashSet<ITypedNPCAttribute> SubAttributes { get; set; } = new();

    public bool Equals(NPCAttribute other)
    {
        var thisArray = this.SubAttributes.ToArray();
        var otherArray = other.SubAttributes.ToArray();
        if (thisArray.Length != otherArray.Length) { return false; }
        else
        {
            for (int i = 0; i < thisArray.Length; i++)
            {
                if (thisArray[i].Type != otherArray[i].Type) { return false; }
                if (!thisArray[i].Equals(otherArray[i])) { return false; }
            }
        }
        return true;
    }

    #region Group-Type Attribute Manipulation
    public static HashSet<NPCAttribute> SpreadGroupTypeAttributes(HashSet<NPCAttribute> attributeList, HashSet<AttributeGroup> groupDefinitions)
    {
        HashSet<NPCAttribute> output = new HashSet<NPCAttribute>();

        foreach (var att in attributeList)
        {
            var groupAttributes = att.SubAttributes.Where(x => x.Type == NPCAttributeType.Group).ToHashSet();
            if (!groupAttributes.Any())
            {
                output.Add(att);
            }

            var additionalSubAttributes = att.SubAttributes.Where(x => x.Type != NPCAttributeType.Group).ToHashSet(); // to be merged into output attributes

            foreach (var IGroup in groupAttributes)
            {
                var group = (NPCAttributeGroup)IGroup;
                foreach (var label in group.SelectedLabels)
                {
                    var subattributesFromGroup = GetGroupedAttributesByLabel(label, groupDefinitions, group.ForceIf);

                    foreach (var subAtt in subattributesFromGroup)
                    {
                        var newAttribute = new NPCAttribute();
                        newAttribute.SubAttributes.UnionWith(additionalSubAttributes);
                        newAttribute.SubAttributes.Add(subAtt);
                        output.Add(newAttribute);
                    }
                }
            }
        }

        return output;
    }

    public static HashSet<ITypedNPCAttribute> GetGroupedAttributesByLabel(string label, HashSet<AttributeGroup> groupDefinitions, bool groupForceIf)
    {
        if (PatcherSettings.General.OverwritePluginAttGroups)
        {
            var matchedMainGroup = PatcherSettings.General.AttributeGroups.Where(x => x.Label == label).FirstOrDefault();
            if (matchedMainGroup != null)
            {
                return GetGroupedAttributesFromGroup(matchedMainGroup, groupDefinitions, groupForceIf);
            }
        }

        // fall back to plugin-supplied group definitions if necessary
        var matchedPluginGroup = groupDefinitions.Where(x => x.Label == label).FirstOrDefault();
        if (matchedPluginGroup != null)
        {
            return GetGroupedAttributesFromGroup(matchedPluginGroup, groupDefinitions, groupForceIf);
        }
        return new HashSet<ITypedNPCAttribute>();
    }

    public static HashSet<ITypedNPCAttribute> GetGroupedAttributesFromGroup(AttributeGroup group, HashSet<AttributeGroup> groupDefinitions,  bool groupForceIf)
    {
        HashSet<ITypedNPCAttribute> outputs = new HashSet<ITypedNPCAttribute>();
        foreach (var attribute in group.Attributes)
        {
            foreach (var subAttribute in attribute.SubAttributes)
            {
                if (subAttribute.Type == NPCAttributeType.Group)
                {
                    var subGroup = (NPCAttributeGroup)subAttribute;
                    foreach (var subLabel in subGroup.SelectedLabels)
                    {
                        outputs.UnionWith(GetGroupedAttributesByLabel(subLabel, groupDefinitions, groupForceIf));
                    }
                }
                else
                {
                    var clonedSubAttribute = CloneAsNew(subAttribute);
                    if (groupForceIf)
                    {
                        clonedSubAttribute.ForceIf = true;
                    }
                    else
                    {
                        clonedSubAttribute.ForceIf = false;
                    }
                    outputs.Add(clonedSubAttribute);
                }
            }
        }
        return outputs;
    }
    #endregion

    public static NPCAttribute CloneAsNew(NPCAttribute input)
    {
        NPCAttribute output = new NPCAttribute();
        foreach (var subAttribute in input.SubAttributes)
        {
            output.SubAttributes.Add(CloneAsNew(subAttribute));
        }
        return output;
    }

    public string ToLogString()
    {
        return "{" + string.Join(" AND ", SubAttributes.Select(x => x.ToLogString())) + "}";
    }

    public static ITypedNPCAttribute CloneAsNew(ITypedNPCAttribute inputInterface)
    {
        switch(inputInterface.Type)
        {
            case NPCAttributeType.Class: return NPCAttributeClass.CloneAsNew((NPCAttributeClass)inputInterface);
            case NPCAttributeType.Custom: return NPCAttributeCustom.CloneAsNew((NPCAttributeCustom)inputInterface);
            case NPCAttributeType.FaceTexture: return NPCAttributeFaceTexture.CloneAsNew((NPCAttributeFaceTexture)inputInterface);
            case NPCAttributeType.Faction: return NPCAttributeFactions.CloneAsNew((NPCAttributeFactions)inputInterface);
            case NPCAttributeType.Group: return NPCAttributeGroup.CloneAsNew((NPCAttributeGroup)inputInterface);
            case NPCAttributeType.NPC: return NPCAttributeNPC.CloneAsNew((NPCAttributeNPC)inputInterface);
            case NPCAttributeType.Race: return NPCAttributeRace.CloneAsNew((NPCAttributeRace)inputInterface);
            case NPCAttributeType.VoiceType: return NPCAttributeVoiceType.CloneAsNew((NPCAttributeVoiceType)inputInterface);
            default: return null;
        }
    }

    // Grouped Sub Attributes get merged together. E.g:
    // Parent has attributes (A && B) || (C && D)
    // Child has attributes (E && F) || (G && H)
    // After inheriting, child will have attributes (A && B && E && F) || (A && B && G && H) || (C && D && E && F) || (C && D && G && H)
    public static HashSet<NPCAttribute> InheritAttributes(HashSet<NPCAttribute> inheritFrom, HashSet<NPCAttribute> inherits)
    {
        var mergedAttributes = new HashSet<NPCAttribute>();

        if (inheritFrom.Count > 0 && inherits.Count == 0)
        {
            return inheritFrom;
        }
        else if (inherits.Count > 0 && inheritFrom.Count == 0)
        {
            return inherits;
        }
        else
        {
            foreach (var childAttribute in inherits)
            {
                foreach (var parentAttribute in inheritFrom)
                {
                    var combinedAttribute = new NPCAttribute();

                    foreach (var subParentAttribute in parentAttribute.SubAttributes)
                    {
                        combinedAttribute.SubAttributes.Add(subParentAttribute);
                    }

                    foreach (var subChildAttribute in childAttribute.SubAttributes)
                    {
                        combinedAttribute.SubAttributes.Add(subChildAttribute);
                    }

                    mergedAttributes.Add(combinedAttribute);
                }
            }
        }

        return mergedAttributes;
    }

    public static string FormKeyToLogStringNamed<T>(FormKey fk) where T: class, IMajorRecordGetter, INamedGetter
    {
        string output = fk.ToString();
        if (PatcherEnvironmentProvider.Instance.Environment.LinkCache.TryResolve<T>(fk, out var getter))
        {
            if (getter.Name != null && !string.IsNullOrWhiteSpace(getter.Name.ToString()))
            {
                output = getter.Name.ToString();
            }
            else if (getter.EditorID != null && !string.IsNullOrWhiteSpace(getter.EditorID))
            {
                output = getter.EditorID;
            }
        }
        return output;
    }

    public static string FormKeyToLogStringUnnamed<T>(FormKey fk) where T : class, IMajorRecordGetter
    {
        string output = fk.ToString();
        if (PatcherEnvironmentProvider.Instance.Environment.LinkCache.TryResolve<T>(fk, out var getter) && getter.EditorID != null && !string.IsNullOrWhiteSpace(getter.EditorID))
        {
            output = getter.EditorID;
        }
        return output;
    }
}

public enum NPCAttributeType
{
    Class,
    Custom,
    Faction,
    FaceTexture,
    Group,
    NPC,
    Race,
    VoiceType
}
public enum CustomAttributeType // moved outside of NPCAttributeCustom so that it can be visible to UC_NPCAttributeCustom's View binding
{
    Text,
    Integer,
    Decimal,
    Boolean,
    Record
}
public class NPCAttributeVoiceType : ITypedNPCAttribute
{
    public HashSet<FormKey> FormKeys { get; set; } = new();
    public NPCAttributeType Type { get; set; } = NPCAttributeType.VoiceType;
    public bool ForceIf { get; set; } = false;
    public int Weighting { get; set; } = 1;

    public bool Equals(ITypedNPCAttribute other)
    {
        var otherTyped = (NPCAttributeVoiceType)other;
        if (this.Type == other.Type && FormKeyHashSetComparer.Equals(this.FormKeys, otherTyped.FormKeys)) { return true; }
        return false;
    }

    public static NPCAttributeVoiceType CloneAsNew(NPCAttributeVoiceType input)
    {
        var output = new NPCAttributeVoiceType();
        output.ForceIf = input.ForceIf;
        output.Type = input.Type;
        output.FormKeys = input.FormKeys;
        output.Weighting = input.Weighting;
        return output;
    }

    public string ToLogString()
    {
        if (PatcherSettings.General.VerboseModeDetailedAttributes)
        {
            return "VoiceType: [" + string.Join(", ", FormKeys.Select(x => NPCAttribute.FormKeyToLogStringUnnamed<IVoiceTypeGetter>(x))) + "]";
        }
        else
        {
            return "VoiceType: [" + string.Join(", ", FormKeys.Select(x => x.ToString())) + "]";
        }
    }
}

public class NPCAttributeClass : ITypedNPCAttribute
{
    public HashSet<FormKey> FormKeys { get; set; } = new();
    public NPCAttributeType Type { get; set; } = NPCAttributeType.Class;
    public bool ForceIf { get; set; } = false;
    public int Weighting { get; set; } = 1;

    public bool Equals(ITypedNPCAttribute other)
    {
        var otherTyped = (NPCAttributeClass)other;
        if (this.Type == other.Type && FormKeyHashSetComparer.Equals(this.FormKeys, otherTyped.FormKeys)) { return true; }
        return false;
    }

    public static NPCAttributeClass CloneAsNew(NPCAttributeClass input)
    {
        var output = new NPCAttributeClass();
        output.ForceIf = input.ForceIf;
        output.Type = input.Type;
        output.FormKeys = input.FormKeys;
        output.Weighting = input.Weighting;
        return output;
    }

    public string ToLogString()
    {
        if (PatcherSettings.General.VerboseModeDetailedAttributes)
        {
            return "Class: [" + string.Join(", ", FormKeys.Select(x => NPCAttribute.FormKeyToLogStringUnnamed<IClassGetter>(x))) + "]";
        }
        else
        {
            return "Class: [" + string.Join(", ", FormKeys.Select(x => x.ToString())) + "]";
        }
    }
}

public class NPCAttributeCustom : ITypedNPCAttribute
{
    public string Path { get; set; } = "";
    public string ValueStr { get; set; } = "";
    public HashSet<FormKey> ValueFKs { get; set; } = new();
    public CustomAttributeType CustomType { get; set; } = CustomAttributeType.Text;
    public string Comparator { get; set; }
    public NPCAttributeType Type { get; set; } = NPCAttributeType.Custom;
    public bool ForceIf { get; set; } = false;
    public int Weighting { get; set; } = 1;
    public FormKey ReferenceNPCFK { get; set; } // this is not used by the patcher but saving it avoids making the user reselect it in the UI
    public Type SelectedFormKeyType { get; set; } // this is not used by the patcher but saving it avoids making the user reselect it in the UI

    public bool Equals(ITypedNPCAttribute other)
    {
        var otherTyped = (NPCAttributeCustom)other;
        if (otherTyped.CustomType != this.CustomType) { return false; }
        if (otherTyped.Path != this.Path) { return false; }
        if (this.CustomType == CustomAttributeType.Record && !FormKeyHashSetComparer.Equals(this.ValueFKs, otherTyped.ValueFKs))
        {
            return false;
        }
        else if (this.ValueStr != otherTyped.ValueStr)
        {
            return false;
        }
        return true;
    }

    public static NPCAttributeCustom CloneAsNew(NPCAttributeCustom input)
    {
        var output = new NPCAttributeCustom();
        output.CustomType = input.CustomType;
        output.ForceIf = input.ForceIf;
        output.Path = input.Path;
        output.Type = input.Type;
        if (input.CustomType == CustomAttributeType.Record)
        {
            output.ValueFKs = new HashSet<FormKey>();
            foreach (var fk in input.ValueFKs)
            {
                output.ValueFKs.Add(new FormKey(fk.ModKey, fk.ID));
            }
        }
        else
        {
            output.ValueStr = input.ValueStr;
        }
        output.Comparator = input.Comparator;
        output.Weighting = input.Weighting;
        return output;
    }

    public string ToLogString()
    {
        string logStr =  "\tCustom ";
        switch(CustomType)
        {
            case CustomAttributeType.Integer: logStr += "Integer: " + Path + " " + Comparator + " " + ValueStr; break;
            case CustomAttributeType.Decimal: logStr += "Decimal: " + Path + " " + Comparator + " " + ValueStr; break;
            case CustomAttributeType.Boolean: logStr += "Boolean: " + Path + " " + Comparator + " " + ValueStr; break;
            case CustomAttributeType.Text: logStr += "Text: " + Path + " " + Comparator + " " + ValueStr; break;
            case CustomAttributeType.Record: logStr += "Record: " + Path + " " + Comparator + " " + String.Join(", ", ValueFKs.Select(x => x.ToString())); break; // probably not worth using a generic TryResolve (or using reflection to get type) just for logging
        }
        return logStr;
    }
}

public class NPCAttributeFactions : ITypedNPCAttribute
{
    public HashSet<FormKey> FormKeys { get; set; } = new();
    public int RankMin { get; set; } = -1;
    public int RankMax { get; set; } = 100;
    public NPCAttributeType Type { get; set; } = NPCAttributeType.Faction;
    public bool ForceIf { get; set; } = false;
    public int Weighting { get; set; } = 1;

    public bool Equals(ITypedNPCAttribute other)
    {
        var otherTyped = (NPCAttributeFactions)other;
        if (this.Type == other.Type && FormKeyHashSetComparer.Equals(this.FormKeys, otherTyped.FormKeys) && this.RankMin == otherTyped.RankMin && this.RankMax == otherTyped.RankMax) { return true; }

        return false;
    }

    public static NPCAttributeFactions CloneAsNew(NPCAttributeFactions input)
    {
        var output = new NPCAttributeFactions();
        output.ForceIf = input.ForceIf;
        output.Type = input.Type;
        output.FormKeys = input.FormKeys;
        output.RankMin = input.RankMin;
        output.RankMax = input.RankMax;
        output.Weighting = input.Weighting;
        return output;
    }

    public string ToLogString()
    {
        if (PatcherSettings.General.VerboseModeDetailedAttributes)
        {
            return "Factions: [" + string.Join(", ", FormKeys.Select(x => NPCAttribute.FormKeyToLogStringNamed<IFactionGetter>(x))) + "] Rank: " + RankMin + " - " + RankMax;
        }
        else
        {
            return "Factions: [" + string.Join(", ", FormKeys.Select(x => x.ToString())) + "] Rank: " + RankMin + " - " + RankMax;
        }
    }
}

public class NPCAttributeFaceTexture : ITypedNPCAttribute
{
    public HashSet<FormKey> FormKeys { get; set; } = new();
    public NPCAttributeType Type { get; set; } = NPCAttributeType.FaceTexture;
    public bool ForceIf { get; set; } = false;
    public int Weighting { get; set; } = 1;

    public bool Equals(ITypedNPCAttribute other)
    {
        var otherTyped = (NPCAttributeFaceTexture)other;
        if (this.Type == other.Type && FormKeyHashSetComparer.Equals(this.FormKeys, otherTyped.FormKeys)) { return true; }
        return false;
    }

    public static NPCAttributeFaceTexture CloneAsNew(NPCAttributeFaceTexture input)
    {
        var output = new NPCAttributeFaceTexture();
        output.ForceIf = input.ForceIf;
        output.Type = input.Type;
        output.FormKeys = input.FormKeys;
        output.Weighting = input.Weighting;
        return output;
    }

    public string ToLogString()
    {
        if (PatcherSettings.General.VerboseModeDetailedAttributes)
        {
            return "Face Texture: [" + string.Join(", ", FormKeys.Select(x => NPCAttribute.FormKeyToLogStringUnnamed<ITextureSetGetter>(x))) + "]";
        }
        else
        {
            return "Face Texture: [" + string.Join(", ", FormKeys.Select(x => x.ToString())) + "]";
        }
    }
}

public class NPCAttributeRace : ITypedNPCAttribute
{
    public HashSet<FormKey> FormKeys { get; set; } = new();
    public NPCAttributeType Type { get; set; } = NPCAttributeType.Race;
    public bool ForceIf { get; set; } = false;
    public int Weighting { get; set; } = 1;

    public bool Equals(ITypedNPCAttribute other)
    {
        var otherTyped = (NPCAttributeRace)other;
        if (this.Type == other.Type && FormKeyHashSetComparer.Equals(this.FormKeys, otherTyped.FormKeys)) { return true; }
        return false;
    }

    public static NPCAttributeRace CloneAsNew(NPCAttributeRace input)
    {
        var output = new NPCAttributeRace();
        output.ForceIf = input.ForceIf;
        output.Type = input.Type;
        output.FormKeys = input.FormKeys;
        output.Weighting = input.Weighting;
        return output;
    }

    public string ToLogString()
    {
        if (PatcherSettings.General.VerboseModeDetailedAttributes)
        {
            return "Race: [" + string.Join(", ", FormKeys.Select(x => NPCAttribute.FormKeyToLogStringNamed<IRaceGetter>(x))) + "]";
        }
        else
        {
            return "Race: [" + string.Join(", ", FormKeys.Select(x => x.ToString())) + "]";
        }
    }
}

public class NPCAttributeNPC : ITypedNPCAttribute
{
    public HashSet<FormKey> FormKeys { get; set; } = new();
    public NPCAttributeType Type { get; set; } = NPCAttributeType.NPC;
    public bool ForceIf { get; set; } = false;
    public int Weighting { get; set; } = 1;

    public bool Equals(ITypedNPCAttribute other)
    {
        var otherTyped = (NPCAttributeNPC)other;
        if (this.Type == other.Type && FormKeyHashSetComparer.Equals(this.FormKeys, otherTyped.FormKeys)) { return true; }
        return false;
    }

    public static NPCAttributeNPC CloneAsNew(NPCAttributeNPC input)
    {
        var output = new NPCAttributeNPC();
        output.ForceIf = input.ForceIf;
        output.Type = input.Type;
        output.FormKeys = input.FormKeys;
        output.Weighting = input.Weighting;
        return output;
    }

    public string ToLogString()
    {
        if (PatcherSettings.General.VerboseModeDetailedAttributes)
        {
            return "NPC: [" + string.Join(", ", FormKeys.Select(x => NPCAttribute.FormKeyToLogStringNamed<INpcGetter>(x))) + "]";
        }
        else
        {
            return "NPC: [" + string.Join(", ", FormKeys.Select(x => x.ToString())) + "]";
        }
    }
}

public class NPCAttributeGroup : ITypedNPCAttribute
{
    public HashSet<string> SelectedLabels { get; set; } = new();
    public NPCAttributeType Type { get; set; } = NPCAttributeType.Group;
    public bool ForceIf { get; set; } = false;
    public int Weighting { get; set; } = 1;

    public bool Equals(ITypedNPCAttribute other)
    {
        if (this.Type == other.Type)
        {
            var otherTyped = (NPCAttributeGroup)other;
            int counter = 0;
            foreach (var s in otherTyped.SelectedLabels)
            {
                if (this.SelectedLabels.Contains(s))
                {
                    counter++;
                }
            }
            if (counter == this.SelectedLabels.Count)
            {
                return true;
            }
        }
            
        return false;
    }

    public static NPCAttributeGroup CloneAsNew(NPCAttributeGroup input)
    {
        var output = new NPCAttributeGroup();
        output.ForceIf = input.ForceIf;
        output.Type = input.Type;
        output.SelectedLabels = input.SelectedLabels;
        output.Weighting = input.Weighting;
        return output;
    }

    public string ToLogString()
    {
        return "Group: [" + string.Join(", ", SelectedLabels) + "]";
    }
}

public interface ITypedNPCAttribute
{
    NPCAttributeType Type { get; set; }
    bool Equals(ITypedNPCAttribute other);
    public bool ForceIf { get; set; }
    public int Weighting { get; set; }
    public string ToLogString();
}

public class AttributeGroup
{
    public string Label { get; set; } = "";
    public HashSet<NPCAttribute> Attributes { get; set; } = new();
}