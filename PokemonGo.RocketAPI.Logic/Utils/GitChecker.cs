﻿#region

using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Logger = PokemonGo.RocketAPI.Logic.Logging.Logger;
using LogLevel = PokemonGo.RocketAPI.Logic.Logging.LogLevel;

#endregion


namespace PokemonGo.RocketAPI.Logic.Utils
{
    public static class GitChecker
    {
        public static string CurrentVersion = $"{Assembly.GetExecutingAssembly().GetName().Version}";
        public static void CheckVersion()
        {
            try
            {
                var match =
                    new Regex(
                        @"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]")
                        .Match(DownloadServerVersion());

                if (!match.Success) return;
                var gitVersion =
                    new Version(
                        $"{match.Groups[1]}.{match.Groups[2]}.{match.Groups[3]}.{match.Groups[4]}");
                
                if (gitVersion >= Assembly.GetExecutingAssembly().GetName().Version)
                {
                    Logger.Write(
                        "You have the Latest Version! " +
                        Assembly.GetExecutingAssembly().GetName().Version, LogLevel.Info);
                    return;
                }

                Logger.Write("There is a new Version Available: https://github.com/xUnholy/Pokemon-Go-Ethereal", LogLevel.Info);
                Logger.Write($"GitHub Version: {gitVersion} | Local Version: {CurrentVersion}", LogLevel.Info);
                Thread.Sleep(1000);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static string DownloadServerVersion()
        {
            //test
            using (var wC = new WebClient())
                return
                    wC.DownloadString(
                        "https://raw.githubusercontent.com/xUnholy/Pokemon-Go-Ethereal/master/PokemonGo.RocketAPI.Logic/Properties/AssemblyInfo.cs");
        }
    }
}