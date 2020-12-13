using CloudTransferTask.src.classes.helper;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Security.Principal;

namespace CloudTransferTask.src.classes {

    class Json {

        public static string confPath = "";
        public static string confFullPath = "";
        public static string confFileName = "config.json";
        private static string folderNameCap = "CloudTransferTasks";
        private static string folderNameNoCap = "cloudtransfertasks";
        private static string folderNameService = "Service";
        private static string confPathLnx = Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar + ".config" + Path.DirectorySeparatorChar;
        private static string confPathOsx = Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar + Environment.UserName + Path.DirectorySeparatorChar + 
            "Library" + Path.DirectorySeparatorChar + "Application Support" + Path.DirectorySeparatorChar;
        private static string confPathWin = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar;
        private static readonly string appdataPathRegex = @"(Users\\)(.*)(\\AppData)";


        /// <summary>
        /// Detect which OS this machine is running
        /// </summary>
        /// <returns></returns>
        public static string DetectOS() {
            string os = "";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                os = "win";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                os = "lin";
                folderNameService = folderNameService.ToLower();
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                os = "osx";
                folderNameService = folderNameService.ToLower();
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) {
                os = "fbd";
                folderNameService = folderNameService.ToLower();
            }

            return os;
        }


        /// <summary>
        /// Set the config path field to read from it later.
        /// </summary>
        public static void SetConfPath() {
            switch (Program.os) {
                case "win":

                    // === If run as windows service set default path to replace it later on
                    using (var identity = WindowsIdentity.GetCurrent()) {
                        if (identity.IsSystem) {
                            confPathWin = "C:" + Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar +
                                    "User" + Path.DirectorySeparatorChar + "AppData" + Path.DirectorySeparatorChar + "Roaming" + Path.DirectorySeparatorChar;
                        }
                    }

                    confPathWin += folderNameCap;
                    confPath = confPathWin;
                    break;
                case "lin":
                case "fbd":
                    confPathLnx += folderNameNoCap;
                    confPath = confPathLnx;
                    break;
                case "osx":
                    confPathOsx += folderNameCap;
                    confPath = confPathOsx;
                    break;
            }

            confFullPath = confPath + Path.DirectorySeparatorChar + confFileName;
        }


        /// <summary>
        /// Write config to file
        /// </summary>
        public static void WriteConfig() {
            var serviceFolder = Environment.CurrentDirectory + Path.DirectorySeparatorChar + folderNameService;

            if (!Directory.Exists(confPath) && !string.IsNullOrEmpty(confPath)) {
                ConsoleLogger.Info("Config folder has been created " + confPath);
                Directory.CreateDirectory(confPath);
            }

            if (!File.Exists(confFullPath)) {
                try {
                    var config = new JsonConfig();
                    var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    
                    try {
                        File.WriteAllText(confFullPath, json);
                        ConsoleLogger.Info("Configuration has been written to " + confFullPath);
                    } catch { }
                } catch (Exception e) {
                    ConsoleLogger.Error(e.ToString());
                }
            }

            try {
                if (!Directory.Exists(serviceFolder)) {
                    Directory.CreateDirectory(serviceFolder);
                }
            } catch (Exception e) {
                Console.WriteLine("ERROR: Cannot create service directory!\n" + e.ToString());
            }
        }


        /// <summary>
        /// Return the configFullPath of all user
        /// </summary>
        /// <returns></returns>
        public static List<string> GetConfigPathOfAllUsers() {
            var returnVal = new List<string>();
            switch (Program.os) {
                case "win":
                    var search = new ManagementObjectSearcher(new SelectQuery("Select * from Win32_UserAccount WHERE Disabled=False"));
                    foreach (ManagementObject env in search.Get()) {
                        var currentUserName = env["Name"].ToString();
                        var configPathOfUser = Regex.Replace(confPathWin, appdataPathRegex, m => m.Groups[1] + currentUserName + m.Groups[3]);
                        returnVal.Add(configPathOfUser);
                    }

                    break;
                case "lin":
                case "fbd":
                    break;
                case "osx":
                    break;
            }

            return returnVal;
        }


        /// <summary>
        /// Get a list of Jobs matching the jobName from the config file
        /// </summary>
        /// <param name="configFilePath">The path to the config file</param>
        /// <param name="jobName">The name of the job</param>
        /// <returns></returns>
        public static List<Jobs> GetJobListFromName(string configFilePath, string jobName) {
            string json = File.ReadAllText(configFilePath);
            var serializerSettings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
            var deserializedJson = JsonConvert.DeserializeObject<JsonConfig>(json, serializerSettings);
            var job = deserializedJson.Jobs.Where(x => x.Name == jobName).ToList();
            return job;
        }


        /// <summary>
        /// Get a list of Jobs matching the source directory from the config file
        /// </summary>
        /// <param name="configFilePath">The path to the config file</param>
        /// <param name="sourceDir">The name of the job</param>
        /// <returns></returns>
        public static List<Jobs> GetJobListFromSourceDir(string configFilePath, string sourceDir) {
            string json = File.ReadAllText(configFilePath);
            var serializerSettings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
            var deserializedJson = JsonConvert.DeserializeObject<JsonConfig>(json, serializerSettings);
            var job = deserializedJson.Jobs.Where(x => x.Source == sourceDir).ToList();
            return job;
        }


        /// <summary>
        /// Get a list of Jobs matching the source directory from the config file
        /// </summary>
        /// <param name="configFilePath">The path to the config file</param>
        /// <param name="jobName">The name of the job</param>
        /// <returns></returns>
        public static List<Jobs> GetJobListFromEnabledService(string configFilePath) {
            string json = File.ReadAllText(configFilePath);
            var serializerSettings = new JsonSerializerSettings() { ObjectCreationHandling = ObjectCreationHandling.Replace };
            var deserializedJson = JsonConvert.DeserializeObject<JsonConfig>(json, serializerSettings);
            var jobs = deserializedJson.Jobs.Where(x => x.Service != null && x.Service.EnableBackgroundService).ToList();
            return jobs;
        }
    }
}
