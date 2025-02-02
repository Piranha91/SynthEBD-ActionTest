﻿using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;

namespace SynthEBD;

class BlockListHandler
{
    public static BlockedNPC GetCurrentNPCBlockStatus(BlockList blockList, FormKey npcFormKey)
    {
        var output = blockList.NPCs.Where(x => x.FormKey == npcFormKey).FirstOrDefault();

        if (output == null)
        {
            output = new BlockedNPC();
            output.Assets = false;
            output.BodyShape = false;
            output.Height = false;
        }

        return output;
    }

        
    public static BlockedPlugin GetCurrentPluginBlockStatus(BlockList blockList, FormKey npcFormKey)
    {
        var contexts = PatcherEnvironmentProvider.Instance.Environment.LinkCache.ResolveAllContexts<INpc, INpcGetter>(npcFormKey).ToList(); // [0] is winning override. [Last] is source plugin

        var output = new BlockedPlugin();
        output.Assets = false;
        output.BodyShape = false;
        output.Height = false;

        foreach (var modKey in contexts.Select(x => x.ModKey))
        {
            var blockedPlugin = blockList.Plugins.Where(x => x.ModKey == modKey).FirstOrDefault();
            if (blockedPlugin != null)
            {
                if (blockedPlugin.Assets)
                {
                    output.Assets = true;
                }
                if (blockedPlugin.BodyShape)
                {
                    output.BodyShape = true;
                }
                if (blockedPlugin.Height)
                {
                    output.Height = true;
                }
            }
        }
           
        return output;
    }
        
}