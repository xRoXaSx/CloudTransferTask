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
        private static string confPathLnx = "/home/" + Environment.UserName + "/.config/";
        private static string confPathOsx = "/Users/" + Environment.UserName + "/Library/Application Support";
        private static string confPathWin = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar;
        //private static string rcloneConfPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar + ".config" + Path.DirectorySeparatorChar + "rclone" + Path.DirectorySeparatorChar + "rclone.conf";
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
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                os = "mox";
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

                    //if (Environment.UserName == Environment.MachineName + "$") {
                    //        confPathWin = "C:" + Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar +
                    //            "User" + Path.DirectorySeparatorChar + "AppData" + Path.DirectorySeparatorChar + "Roaming" + Path.DirectorySeparatorChar;                        
                    //}

                    using (var identity = WindowsIdentity.GetCurrent()) {
                        if (identity.IsSystem) {
                            confPathWin = "C:" + Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar +
                                    "User" + Path.DirectorySeparatorChar + "AppData" + Path.DirectorySeparatorChar + "Roaming" + Path.DirectorySeparatorChar;

                            // === To get rclone config automatically
                            //rcloneConfPath = "C:" + Path.DirectorySeparatorChar + "Users" + Path.DirectorySeparatorChar + 
                            //    "User" + Path.DirectorySeparatorChar + ".config" + Path.DirectorySeparatorChar + "rclone" + Path.DirectorySeparatorChar + "rclone.conf";
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
                case "mox":
                    confPathOsx += folderNameCap;
                    confPath = confPathOsx;
                    break;
            }

            confFullPath = confPath + Path.DirectorySeparatorChar + confFileName;
        }

        public static void WriteConfig() {
            if (!Directory.Exists(confPath) && !string.IsNullOrEmpty(confPath)) {
                ConsoleLogger.Info("Config folder has been created " + confPath);
                Directory.CreateDirectory(confPath);
            }

            if (!File.Exists(confFullPath)) {
                try {
                    var config = new JsonConfig();
                    //config.RCloneConfigfileLocation = rcloneConfigLocation;
                    var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    var serviceFolder = Environment.CurrentDirectory + Path.DirectorySeparatorChar + folderNameService;

                    try {
                        if (!Directory.Exists(serviceFolder)) {
                            Directory.CreateDirectory(serviceFolder);
                        }
                    } catch (Exception e) {
                        Console.WriteLine("ERROR: Cannot create service directory!\n" + e.ToString());
                    }
                    
                    try {
                        File.WriteAllText(confFullPath, json);
                        ConsoleLogger.Info("Configuration has been written to " + confFullPath);
                    } catch { }
                } catch (Exception e) {
                    ConsoleLogger.Error(e.ToString());
                }
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
                case "mos":
                    break;
            }

            return returnVal;
        }


        /// <summary>
        /// To get rclone config automatically. Return the configFullPath of all user
        /// </summary>
        /// <returns></returns>
        //public static string GetRCloneConfigLocationOfUser(string cloudTransferTaskConfigPath) {
        //    var returnVal = "";
        //    switch (Program.os) {
        //        case "win":
        //            var user = Regex.Match(appdataPathRegex, cloudTransferTaskConfigPath);
        //            if (user.Success) {
        //                var configPathOfUser = Regex.Replace(rcloneConfPath, @"(Users\\)(.*)(\\\.config)", m => m.Groups[1].ToString() + user.Groups[1].ToString() + m.Groups[3]);
        //                if (!File.Exists(configPathOfUser)) {
        //                    ConsoleLogger.Error("Cannot get the users config file!");
        //                } 
        //            }

        //            break;
        //        case "lin":
        //        case "fbd":
        //            break;
        //        case "mos":
        //            break;
        //    }

        //    return returnVal;
        //}


        /// <summary>
        /// Get the rclone configuration location (for creating the CloudTransferTask config file)
        /// </summary>
        /// <returns></returns>
        //private static string GetRcloneConfigLocation() {
        //    var returVal = "";
            
        //    if (File.Exists(rcloneConfPath)) {
        //        returVal = rcloneConfPath;
        //    } else {
        //        returVal = "Cannot determine rclone config location! Please run the following rclone command and copy the filepath in here. \"rclone config file\"";
        //    }

        //    return returVal;
        //}


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
            var jobs = deserializedJson.Jobs.Where(x => x.EnableBackgroundService).ToList();
            return jobs;
        }
    }
}
