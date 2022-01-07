﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthEBD
{
    class SettingsIO_Height
    {
        public static Settings_Height LoadHeightSettings()
        {
            Settings_Height heightSettings = new Settings_Height();

            if (File.Exists(PatcherSettings.Paths.HeightSettingsPath))
            {
                string text = File.ReadAllText(PatcherSettings.Paths.HeightSettingsPath);
                heightSettings = JsonConvert.DeserializeObject<Settings_Height>(text);
            }
            else if (File.Exists(PatcherSettings.Paths.GetFallBackPath(PatcherSettings.Paths.HeightSettingsPath)))
            {
                string text = File.ReadAllText(PatcherSettings.Paths.GetFallBackPath(PatcherSettings.Paths.HeightSettingsPath));
                heightSettings = JsonConvert.DeserializeObject<Settings_Height>(text);
            }

            return heightSettings;
        }

        public static List<HeightConfig> loadHeightConfigs()
        {
            List<HeightConfig> loaded = new List<HeightConfig>();

            string searchPath = "";
            if (Directory.Exists(PatcherSettings.Paths.HeightConfigDirPath))
            {
                searchPath = PatcherSettings.Paths.HeightConfigDirPath;
            }
            else if (Directory.Exists(PatcherSettings.Paths.GetFallBackPath(PatcherSettings.Paths.HeightConfigDirPath)))
            {
                // Warn User
                searchPath = PatcherSettings.Paths.GetFallBackPath(PatcherSettings.Paths.HeightConfigDirPath);
            }
            else
            {
                // Warn User
                return loaded;
            }

            string[] filePaths = Directory.GetFiles(searchPath, "*.json");

            foreach (string s in filePaths)
            {
                string text = File.ReadAllText(s);

                if (text.Contains("\"EDID\":")) // zEBD formatted height config
                {
                    try
                    {
                        var zEBDformatted = JSONhandler<HashSet<HeightAssignment.zEBDHeightAssignment>>.loadJSONFile(s);
                        HeightConfig fromZformat = new HeightConfig();
                        fromZformat.Label = Path.GetFileNameWithoutExtension(s);

                        foreach (var zHC in zEBDformatted)
                        {
                            var ha = new HeightAssignment();
                            ha.Label = zHC.EDID;
                            ha.Races = new HashSet<Mutagen.Bethesda.Plugins.FormKey> { Converters.RaceEDID2FormKey(zHC.EDID) };

                            if (float.TryParse(zHC.heightMale, out var maleHeight))
                            {
                                ha.HeightMale = maleHeight;
                            }
                            else
                            {
                                Logger.LogError("Cannot parse male height " + zHC.heightMale + " for Height Assignment: " + ha.Label);
                            }

                            if (float.TryParse(zHC.heightFemale, out var femaleHeight))
                            {
                                ha.HeightFemale = femaleHeight;
                            }
                            else
                            {
                                Logger.LogError("Cannot parse female height " + zHC.heightFemale + " for Height Assignment: " + ha.Label);
                            }

                            if (float.TryParse(zHC.heightMaleRange, out var maleHeightRange))
                            {
                                ha.HeightMaleRange = maleHeightRange;
                            }
                            else
                            {
                                Logger.LogError("Cannot parse male height range " + zHC.heightMaleRange + " for Height Assignment: " + ha.Label);
                            }

                            if (float.TryParse(zHC.heightFemaleRange, out var femaleHeightRange))
                            {
                                ha.HeightFemaleRange = femaleHeightRange;
                            }
                            else
                            {
                                Logger.LogError("Cannot parse female height range " + zHC.heightFemaleRange + " for Height Assignment: " + ha.Label);
                            }

                            fromZformat.HeightAssignments.Add(ha);
                        }

                        fromZformat.FilePath = s;
                        loaded.Add(fromZformat);
                    }
                    catch
                    {
                    }
                }

                else
                {
                    try
                    {
                        var hc = JSONhandler<HeightConfig>.loadJSONFile(s);
                        hc.FilePath = s;
                        loaded.Add(hc);
                    }
                    catch
                    {
                        //Warn User
                    }
                }
            }

            return loaded;
        }

        public static void SaveHeightConfigs(List<HeightConfig> heightConfigs)
        {
            for (int i = 0; i < heightConfigs.Count; i++)
            {
                if (heightConfigs[i].FilePath != "")
                {
                    JSONhandler<HeightConfig>.SaveJSONFile(heightConfigs[i], heightConfigs[i].FilePath);
                }
                else
                {
                    string newPath = "";
                    if (IO_Aux.IsValidFilename(heightConfigs[i].Label))
                    {
                        if (Directory.Exists(PatcherSettings.Paths.HeightConfigDirPath))
                        {
                            newPath = Path.Combine(PatcherSettings.Paths.HeightConfigDirPath, heightConfigs[i].Label + ".json");
                        }
                        else if (Directory.Exists(PatcherSettings.Paths.GetFallBackPath(PatcherSettings.Paths.HeightConfigDirPath)))
                        {
                            newPath = Path.Combine(PatcherSettings.Paths.GetFallBackPath(PatcherSettings.Paths.HeightConfigDirPath), heightConfigs[i].Label + ".json");
                        }

                        JSONhandler<HeightConfig>.SaveJSONFile(heightConfigs[i], newPath);
                    }

                    else
                    {
                        // Configure save file dialog box
                        var dialog = new Microsoft.Win32.SaveFileDialog();
                        dialog.DefaultExt = ".json"; // Default file extension
                        dialog.Filter = "JSON files (.json|*.json"; // Filter files by extension

                        if (Directory.Exists(PatcherSettings.Paths.HeightConfigDirPath))
                        {
                            dialog.InitialDirectory = Path.GetFullPath(PatcherSettings.Paths.HeightConfigDirPath);
                        }
                        else if (Directory.Exists(PatcherSettings.Paths.GetFallBackPath(PatcherSettings.Paths.HeightConfigDirPath)))
                        {
                            dialog.InitialDirectory = Path.GetFullPath(PatcherSettings.Paths.GetFallBackPath(PatcherSettings.Paths.HeightConfigDirPath));
                        }

                        dialog.RestoreDirectory = true;

                        // Show open file dialog box
                        bool? result = dialog.ShowDialog();

                        // Process open file dialog box results
                        if (result == true)
                        {
                            JSONhandler<HeightConfig>.SaveJSONFile(heightConfigs[i], dialog.FileName);
                        }
                    }
                }
            }
        }
    }
}
