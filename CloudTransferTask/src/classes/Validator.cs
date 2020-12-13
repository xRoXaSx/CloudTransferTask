using CloudTransferTask.src.classes.helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CloudTransferTask.src.classes {
    class Validator {

        private static readonly string rcloneConfigPathArguments = "config file";
        private static string rcloneConfigPath;

        /// <summary>
        /// Validate the config file
        /// </summary>
        /// <param name="dryRun">Run as dryrun or overwrite the default config?</param>
        public void Validate(bool dryRun = false) {
            rcloneConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar + ".config" + 
                Path.DirectorySeparatorChar + "rclone" + Path.DirectorySeparatorChar + "rclone.conf";

            try {
                var json = File.ReadAllText(Json.confFullPath);
                var serializerSettings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
                var deserializedJson = JsonConvert.DeserializeObject<JsonConfig>(json, serializerSettings);

                if (deserializedJson.Debug) {
                    ConsoleLogger.ValidationInfo("Debug should only be enabled if you need to debug!");
                } else {
                    ConsoleLogger.ValidationOK("Debug is turned off, good! It only should be turned on if you need to debug!");
                }

                if (!string.IsNullOrEmpty(deserializedJson.RCloneConsoleColor)) {
                    if (Enum.TryParse(typeof(ConsoleColor), char.ToUpper(deserializedJson.RCloneConsoleColor[0]) + deserializedJson.RCloneConsoleColor.ToLower().Substring(1), out object color)) {
                        ConsoleLogger.ValidationOK("RCloneConsoleColor parsed successfully!");
                    } else {
                        deserializedJson.RCloneConsoleColor = "Gray";
                        ConsoleLogger.ValidationError("Cannot parse RCloneConsoleColor! I've set it to Gray");
                    }
                }

                var jobs = new List<Jobs>();
                foreach (var job in deserializedJson.Jobs) {
                    jobs.Add(CheckForEnabledServices(job, deserializedJson.RCloneProgramLocation));
                }

                deserializedJson.Jobs = jobs;

                if (!dryRun) {
                    json = JsonConvert.SerializeObject(deserializedJson, Formatting.Indented);
                    File.WriteAllText(Json.confFullPath, json);
                    ConsoleLogger.Info("Configuration has been overwritten");
                }

                // === Write config back to file


            } catch (Exception e) {
                ConsoleLogger.Error("Cannot validate! " + e.ToString());
            }
        }


        /// <summary>
        /// Check for enabled services and add missing information if not already set
        /// </summary>
        /// <param name="job">The job of the config to check</param>
        /// <param name="rcloneProgramLocation">The location of rclone to get the config location via command</param>
        /// <returns></returns>
        private Jobs CheckForEnabledServices(Jobs job, string rcloneProgramLocation) {
            if (job.Service != null && job.Service.EnableBackgroundService) {
                bool addedConfigParameter = false;
                if (!job.Flags.Any(x => x.Contains("--config"))) {
                    if (File.Exists(rcloneConfigPath)) {
                        // === Append --config to Flags
                        job.Flags.Add("--config " + rcloneConfigPath);
                        addedConfigParameter = true;

                    } else {
                        // === Try to get path via rclone config file
                        try {
                            if (File.Exists(rcloneProgramLocation)) {
                                ProcessStartInfo startInfo = new ProcessStartInfo() {
                                    FileName = rcloneProgramLocation,
                                    UseShellExecute = false,
                                    Arguments = rcloneConfigPathArguments,
                                    RedirectStandardOutput = true
                                };

                                Process proc = new Process() { StartInfo = startInfo };
                                ConsoleLogger.ValidationInfo("Starting rclone to determine config location...\n");
                                var output = proc.StandardOutput.ReadToEnd();

                                proc.Start();

                                using (var reader = new StringReader(output)) {
                                    for (string line = reader.ReadLine(); line != null; line = reader.ReadLine()) {
                                        if (File.Exists(line)) {
                                            rcloneConfigPath = line;
                                        }
                                    }
                                }

                                proc.WaitForExit();

                                if (!string.IsNullOrEmpty(rcloneConfigPath)) {
                                    job.Flags.Add("--config " + rcloneConfigPath);
                                    addedConfigParameter = true;
                                }

                            } else {
                                ConsoleLogger.Warning("File stated in \"RCloneProgramLocation\" cannot be found!");
                            }
                        } catch (Exception ex) {
                            ConsoleLogger.Error(ex.ToString());
                        }

                        ConsoleLogger.Warning("rclone config could not be found!" +
                            "EnableBackgroundService tasks need to have \"--config PathTo" + Path.DirectorySeparatorChar + "Your" + Path.DirectorySeparatorChar +
                            "rclone.conf\" defined in the Flag section");
                    }
                } else {
                    var configFlag = job.Flags.Where(x => x.Contains("--config")).FirstOrDefault();
                    if (configFlag != null) {
                        var configFlagPath = Regex.Match(configFlag, @"--config\s(.*)");
                        
                        if (configFlagPath.Success) {
                            if (File.Exists(configFlagPath.Groups[1].ToString())) {
                                ConsoleLogger.ValidationOK("Parameter \"--config\": configfile \"" + configFlagPath.Groups[1].ToString() + "\" does exist");
                            } else {
                                if (File.Exists(rcloneConfigPath)) {
                                    job.Flags.Where(x => x.Contains("--config")).ToList().ForEach(x => x = "--config " + rcloneConfigPath);
                                    ConsoleLogger.ValidationError("The file entered with the parameter \"--config\" does not exist! Added the default one of user " + Environment.UserName);
                                }
                            }
                        } else {
                            job.Flags.Where(x => x.Contains("--config")).ToList().ForEach(x => x = "--config " + rcloneConfigPath);
                            addedConfigParameter = true;
                            ConsoleLogger.ValidationInfo("Could not find the parameter \"--config\"! Added it...");
                        }
                    } else {
                        job.Flags.Where(x => x.Contains("--config")).ToList().ForEach(x => x = "--config " + rcloneConfigPath);
                        addedConfigParameter = true;
                        ConsoleLogger.ValidationInfo("Could not find the parameter \"--config\"! Added it...");
                    }
                }
            
                if (addedConfigParameter) {
                    ConsoleLogger.ValidationInfo("Added \"--config\" flag for the EnableBackgroundService \"" + job.Name + "\"");
                }
            }

            return job;
        }
    }
}
