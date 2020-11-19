using CloudTransferTask.src.classes;
using CloudTransferTask.src.classes.helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Security.Principal;

namespace CloudTransferTask {
    class Program {

        public static readonly string os = Json.DetectOS();
        public static string configFilePath = "";
        private readonly static List<string> validateDryRunAliases = new List<string>() { "dryrun", "dry", "whatif", "testonly", "onlytest", "test", "try", "check"};
        private static Dictionary<string, List<string>> DateTimeMatch = new Dictionary<string, List<string>>() {
            { "date", new List<string>() { "date", "d" } },
            { "time", new List<string>() { "time", "t" } },
            { "datetime", new List<string>() { "datetime", "dt" } }
        };

        static void Main(string[] args) {
            Json.SetConfPath();
            configFilePath = Json.confPath + Path.DirectorySeparatorChar + Json.confFileName;

            if (args.Length == 0) {
                Json.WriteConfig();
            } else {
                if (args[0].ToLower() == "validate") {
                    if (args.Length > 1) { 
                        if (validateDryRunAliases.Contains(args[1].ToLower())) {
                            new Validator().Validate(true);
                            return;
                        }
                    }

                    new Validator().Validate();
                    return;
                }

                if (!File.Exists(configFilePath)) {
                    ConsoleLogger.Notice("Config does not exist! Creating it...");
                    Json.WriteConfig();
                }

                RunRClone(args);
            }
        }


        private static int RunRClone(string[] args) {
            var isSystem = CheckIfRunAsWindowsSertvice();
            var returnVal = 0;
            var json = File.ReadAllText(configFilePath);
            var serializerSettings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
            var deserializedJson = JsonConvert.DeserializeObject<JsonConfig>(json, serializerSettings);
            var jobs = deserializedJson.Jobs.Where(x => x.Name == args[0]).ToList();

            if (isSystem) {
                try {
                    var configs = Json.GetConfigPathOfAllUsers();
                    jobs = new List<Jobs>();
                    foreach (var config in configs) {
                        json = File.ReadAllText(config + Path.DirectorySeparatorChar + Json.confFileName, Encoding.UTF8);
                        deserializedJson = JsonConvert.DeserializeObject<JsonConfig>(json, serializerSettings);
                        jobs.AddRange(deserializedJson.Jobs.Where(x => x.Name == args[0]).ToList());
                    }
                } catch (Exception e) {
                    ConsoleLogger.Error(e.ToString());
                }
            }

            if (!string.IsNullOrEmpty(deserializedJson.RCloneProgramLocation) && deserializedJson.RCloneProgramLocation != "TheLocationOfRClone") {
                if (jobs.Count == 0) {
                    ConsoleLogger.Notice("Could not find any jobs called " + args[0]);
                } else if (jobs.Count > 1 && !isSystem) {
                    Console.WriteLine("Found more than one (" + jobs.Count + ") job called " + args[0]);
                    Console.WriteLine("--- Jobs ---");
                    foreach (var job_ in jobs) {
                        Console.WriteLine("   Name: " + job_.Name + " | Source: " + job_.Source + " | Destination: " + job_.Destination);
                    }
                } else {
                    foreach(var j in jobs) {
                        if (!Directory.Exists(j.Source)) {
                            ConsoleLogger.Warning("The source \"" + j.Source + "\" of job " + j.Name + " does not exist!");
                            if (!j.PreAction.Enabled || j.PreAction == null || string.IsNullOrEmpty(j.PreAction.MainCommand)) {
                                ConsoleLogger.Error("No pre action has been defined and job source is empty or does not exist!");
                                return 1;
                            }
                        }

                        j.Destination = ReplaceDateTimePlaceholders(j.Destination);

                        if (j.Destination.Contains(" ")) {
                            j.Destination = "\"" + j.Destination + "\"";
                        }

                        if (j.Source.Contains(" ")) {
                            j.Source = "\"" + j.Source + "\"";
                        }


                        // === Pre rclone Actions
                        if (j.PreAction != null) {
                            var pre = j.PreAction;
                            if (pre.Enabled && !string.IsNullOrEmpty(pre.MainCommand)) {
                                if (string.IsNullOrEmpty(pre.FailAfterTimeOut.ToString())) { pre.FailAfterTimeOut = false;}
                                if (string.IsNullOrEmpty(pre.FailAfterTimeOut.ToString())) { pre.FailAfterTimeOut = false;}
                                try {
                                    RunActions(j, pre, deserializedJson.RCloneProgramLocation, "pre action");
                                } catch (Exception e) {
                                    ConsoleLogger.Error(e.ToString());
                                }
                            }
                        }

                        // === rclone Action
                        try {
                            ProcessStartInfo startInfo = new ProcessStartInfo() {
                                FileName = deserializedJson.RCloneProgramLocation,
                                UseShellExecute = false,
                                Arguments = j.Action + " " + j.Source + " " + j.Destination + " " + string.Join(" ", j.FileType.Select(x => "--include " + x)) + " " + string.Join(" ", j.Flags),
                            };


                            if (deserializedJson.Debug) {
                                ConsoleLogger.Debug(
                                    "-- DEBUG -- RCloneProgramLocation: " + deserializedJson.RCloneProgramLocation + 
                                    Environment.NewLine + "-- DEBUG -- Action: " + j.Action +
                                    Environment.NewLine + "-- DEBUG -- Source: " + j.Source + Environment.NewLine + "-- DEBUG -- Destination: " + j.Destination + 
                                    Environment.NewLine + "-- DEBUG -- FileType(s): " + string.Join(" ", j.FileType) + 
                                    Environment.NewLine + "-- DEBUG -- Flags: " + string.Join(" ", j.Flags) +
                                    Environment.NewLine + "-- DEBUG -- Executing Command: " + deserializedJson.RCloneProgramLocation + " " + j.Action + " " +
                                        j.Source + " " + j.Destination + " " + string.Join(" ", j.FileType.Select(x => "--include " + x)) + " " + string.Join(" ", j.Flags)
                                );
                            }

                            Process proc = new Process() { StartInfo = startInfo };
                            ConsoleLogger.Info("Starting process...\n");
                            Console.ForegroundColor = ReplaceConsoleColor(deserializedJson.RCloneConsoleColor);

                            proc.Start();
                            proc.WaitForExit();
                            Console.ResetColor();
                            ConsoleLogger.Info("Finished successfully...");
                        } catch (Exception e) {
                            ConsoleLogger.Error(e.ToString());
                            returnVal = 1;
                            Environment.Exit(1);
                        }


                        // === Post rclone Actions
                        if (j.PostAction != null) {
                            var post = j.PostAction;
                            if (post.Enabled && !string.IsNullOrEmpty(post.MainCommand)) {
                                try {
                                    try {
                                        RunActions(j, post, deserializedJson.RCloneProgramLocation, "post action");
                                    } catch (Exception e) {
                                        if (e.GetType().IsAssignableFrom(typeof(System.ComponentModel.Win32Exception)) && e.Message.ToLower() == "no such file or directory") {
                                            ConsoleLogger.Error("Seems like the given arguments / the \"MainCommand\" is wrong!" + (deserializedJson.Debug ? Environment.NewLine + e.ToString() : ""));
                                        } else {
                                            ConsoleLogger.Error(e.ToString());
                                        }
                                    }
                                } catch (Exception e) {
                                    ConsoleLogger.Error(e.ToString());
                                    returnVal = 1;
                                }
                            }
                        }
                    }
                }
            } else {
                ConsoleLogger.Error("RCloneProgramLocation is not set!");
            }

            Environment.ExitCode = returnVal;
            return returnVal;
        }



        private static bool CheckIfRunAsWindowsSertvice() {
            // === Check if ran as windows service. Linux deamon will run with user context
            var isSystem = false;
            if (os == "win") {
                using (var identity = WindowsIdentity.GetCurrent()) {
                    isSystem = identity.IsSystem;
                }
            }

            return isSystem;
        }


        /// <summary>
        /// Replace the given string with a ConsoleColor (case-insensitive)
        /// </summary>
        /// <param name="consoleColor">The color which should be returned</param>
        /// <returns></returns>
        private static ConsoleColor ReplaceConsoleColor(string consoleColor) {
            var returnValue = ConsoleColor.Gray;
            if (!string.IsNullOrEmpty(consoleColor)) {
                if (consoleColor.ToLower() == "grey") {
                    consoleColor = "gray";
                }

                returnValue = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), char.ToUpper(consoleColor[0]) + consoleColor.ToLower().Substring(1));
            }

            return returnValue;
        }


        /// <summary>
        /// Run a pre or post action
        /// </summary>
        /// <param name="j">The job that is / should be run</param>
        /// <param name="action">The pre / post action</param>
        /// <param name="rcloneProgramLocation">The location of rclone (used for placeholder replacement)</param>
        /// <param name="actionType">Either pre or post action (used for console messages only)</param>
        /// <returns></returns>
        private static int RunActions(Jobs j, Actions action, string rcloneProgramLocation, string actionType) {
            var returnVal = 0;
            ConsoleLogger.Error("Starting " + actionType + "...\n");
            using (Process process = new Process()) {
                var argList = new List<string>();
                argList.AddRange(action.AdditionalArguments.Select(x => x.Replace(@" ", "_")).ToList());

                process.StartInfo.FileName = action.MainCommand;
                process.StartInfo.Arguments = string.Join(" ", ReplaceAdditionalArguments(argList, j, rcloneProgramLocation));
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false)) {

                    process.OutputDataReceived += (sender, e) => {
                        if (e.Data == null) {
                            outputWaitHandle.Set();
                        } else {
                            output.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) => {
                        if (e.Data == null) {
                            errorWaitHandle.Set();
                        } else {
                            error.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (process.WaitForExit(action.MilliSecondsUntillTimeout) && outputWaitHandle.WaitOne(action.MilliSecondsUntillTimeout) &&
                        errorWaitHandle.WaitOne(action.MilliSecondsUntillTimeout)) {

                        // Process completed
                        if (process.ExitCode == 0) {
                            ConsoleLogger.Info("Successfully completed " + actionType);
                        } else {
                            ConsoleLogger.Error(actionType + " has failed!");
                            if (action.FailIfNotSucceeded) {
                                ConsoleLogger.Error("Exiting... FailIfNotSucceeded = " + action.FailIfNotSucceeded);
                                Environment.Exit(returnVal);
                            } else {
                                ConsoleLogger.Notice("Skipping... FailIfNotSucceeded = " + action.FailIfNotSucceeded);
                            }
                        }
                    } else {
                        if (action.FailAfterTimeOut) {
                            ConsoleLogger.Error("Timed out... Exiting... FailAfterTimeOut = " + action.FailAfterTimeOut);
                            Environment.Exit(returnVal);
                        } else {
                            ConsoleLogger.Notice("Timed out... Skipping... FailAfterTimeOut = " + action.FailAfterTimeOut);
                            returnVal = 1;
                        }
                    }
                }
            }

            return returnVal;
        }


        /// <summary>
        /// Replace DateTime placeholders (eg.: &lt;date:yyyy-MM-dd&gt;) with the corresponding values
        /// </summary>
        /// <param name="arg">The string containing the placeholder</param>
        /// <returns></returns>
        private static string ReplaceDateTimePlaceholders(string arg) {
            var returnVal = "";
            foreach (var placeholder in DateTimeMatch) {
                var match = placeholder.Value.Select(x => Regex.Match(arg, "<" + x + ":(.*)>", RegexOptions.IgnoreCase)).FirstOrDefault();
                if (match.Success) {
                    var format = match.Groups[1].Value;
                    var matchedPhrase = match.Groups[0].Value;
                    var now = DateTime.Now;

                    try {
                        returnVal = arg.Replace(matchedPhrase, now.ToString(format));
                    } catch {
                        ConsoleLogger.Error("Unable to convert " + format + " to date and time!");
                        ConsoleLogger.Error("Exiting...");
                        Environment.Exit(1);
                    }
                }
            }

            return returnVal;
        }



        /// <summary>
        /// Replace placeholders in AdditionalArguments in Actions class
        /// </summary>
        /// <param name="args">The AdditionalArguments list</param>
        /// <param name="job">The matched job</param>
        /// <param name="rcloneLocation">The rcloneLocation (needed if it needs to be replaced)</param>
        /// <returns></returns>
        private static List<string> ReplaceAdditionalArguments(List<string> args, Jobs job, string rcloneLocation) {
            List<string> returnArgs = new List<string>();
            foreach (var arg in args) {
                switch (arg) {
                    case string x when x.Contains("<Name>"):
                        returnArgs.Add(arg.Replace("<Name>", job.Name));
                        break;
                    case string x when x.Contains("<Source>"):
                        returnArgs.Add(arg.Replace("<Source>", job.Source));
                        break;
                    case string x when x.Contains("<Destination>"):
                        returnArgs.Add(arg.Replace("<Destination>", job.Destination));
                        break;
                    case string x when x.Contains("<Action>"):
                        returnArgs.Add(arg.Replace("<Action>", job.Action));
                        break;
                    case string x when x.Contains("<FileType>"):
                        returnArgs.Add(arg.Replace("<FileType>", string.Join(",", job.FileType)));
                        break;
                    case string x when x.Contains("<RCloneProgramLocation>"):
                        returnArgs.Add(arg.Replace("<RCloneProgramLocation>", rcloneLocation));
                        break;
                    case string x when x.Contains("<Flags>"):
                        returnArgs.Add(arg.Replace("<Flags>", string.Join(",", job.Flags.Select(x => x.Replace(" ", "_")))));
                        break;
                    default:
                        returnArgs.Add(ReplaceDateTimePlaceholders(arg));
                        break;
                }
            }
            return returnArgs;
        }
    }
}
