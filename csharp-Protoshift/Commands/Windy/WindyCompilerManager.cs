﻿using System.Collections.ObjectModel;
using YYHEggEgg.Logger;
using csharp_Protoshift.Configuration;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace csharp_Protoshift.Commands.Windy
{
    public enum OSType
    {
        Unsupported,
        win32,
        win64,
        mac32,
        mac64,
        linux32,
        linux64,
    }

    internal static partial class WindyLuacManager
    {
        static WindyLuacManager()
        {
            _logger = Log.GetChannel("WindyCompilers");
            LuaCompilersDefault = new(new Dictionary<OSType, string>
            {
                { OSType.win32, "resources/luac_bins/luac_win32.exe" },
                { OSType.win64, "resources/luac_bins/luac_win64.exe" },
                // { OSType.mac32, "resources/luac_bins/luac_mac32" },
                { OSType.mac64, "resources/luac_bins/luac_unix64" },
                // { OSType.linux32, "resources/luac_bins/luac_linux32" },
                { OSType.linux64, "resources/luac_bins/luac_unix64" },
            });
            ChmodAddX();
            _lua_compilers_path = new(LuaCompilersDefault);

            Directory.Delete("windy_temp", true);
            var settmpPathRes = SetCompiledTempPath("windy_temp");
            Debug.Assert(settmpPathRes);
        }

        private static LoggerChannel _logger;

        /// <summary>
        /// Get OS Suffix in combination of "win", "mac", "linux" and "32", "64".
        /// </summary>
        /// <returns>null if unsupported</returns>
        public static OSType GetOSSuffix()
        {
            string arch = Environment.Is64BitOperatingSystem ? "64" : "32";
            if (OperatingSystem.IsWindows()) return Enum.Parse<OSType>($"win{arch}");
            else if (OperatingSystem.IsMacOS()) return Enum.Parse<OSType>($"mac{arch}");
            else if (OperatingSystem.IsLinux()) return Enum.Parse<OSType>($"linux{arch}");
            else return OSType.Unsupported;
        }

        public static readonly ReadOnlyDictionary<OSType, string> LuaCompilersDefault;

        private static void ChmodAddX(string? target = null)
        {
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                target ??= LuaCompilersDefault[GetOSSuffix()];
                try
                {
                    Process.Start("chmod", $"+x {target}")?.WaitForExit();
                }
                catch (Exception ex)
                {
                    _logger.LogWarnTrace(ex, $"chmod +x for luac executable failed. " +
                        $"You will probably encounter problem for Windy operations.");
                }
            }
        }

        private static bool CanRuntimeValidate(OSType targetOS)
        {
            return GetOSSuffix() switch
            {
                OSType.Unsupported => false,
                OSType.win32 => targetOS == OSType.win32,
                OSType.win64 => targetOS == OSType.win64 || targetOS == OSType.win32,
                OSType.mac32 => targetOS == OSType.mac32,
                // You're right, but "macOS 10.15" is a macOS release version
                // independently developed by Apple. The kernel is running in
                // a mystrial world called 'Darwin'. You will act as a mysterious
                // character named as 'macOS Catalina', experience failing to run
                // any x86 apps with various usage and unique abilities, install
                // Parallel Desktop and find the lost Windows features -- and
                // gradually discover the truth about 'M1 Mac'.
                OSType.mac64 => targetOS == OSType.mac64,
                OSType.linux32 => targetOS == OSType.linux32,
                OSType.linux64 => targetOS == OSType.linux64 || targetOS == OSType.linux32,
                _ => false,
            };
        }

        #region OS Compilers manage
        private static Dictionary<OSType, string> _lua_compilers_path;

        public static async Task<bool> TryModifyLuacExecutablePath(
            string luacFullPath, OSType targetOS, bool slient = false)
        {
            if (targetOS == OSType.Unsupported)
            {
                if (!slient)
                {
                    _logger.LogWarn($"OS is not supported to run windy currently.");
                }
                return false;
            }
            if (!slient) _logger.LogInfo($"Verifying luac_{targetOS} at: {luacFullPath}");
            if (CanRuntimeValidate(targetOS))
            {
                ChmodAddX(luacFullPath);
                try
                {
                    var versionInfo = await new WindyOuterInvoke(luacFullPath, "-v").RunAsync();
                    if (!slient) _logger.LogInfo($"Got luac version: {versionInfo}");
                    _lua_compilers_path[targetOS] = luacFullPath;
                    ClearCompiled();
                    return true;
                }
                catch (Exception ex)
                {
                    if (!slient) _logger.LogErroTrace(ex, $"verify luac_{targetOS} failed. ");
                    return false;
                }
            }
            else
            {
                if (!slient)
                {
                    _logger.LogInfo($"Can't validate luac_{targetOS} currently, pass the check.");
                }
                _lua_compilers_path[targetOS] = luacFullPath;
                return true;
            }
        }
        #endregion

        public static void ClearCompiled()
        {
            if (CompiledPath != null)
            {
                lock (lua_dict_lck)
                {
                    Directory.Delete(CompiledPath, true);  
                    compiled_luacs.Clear();
                }
            }
            _logger.LogInfo($"All lua scripts will be recompiled next time sent " +
                $"for the working Luac executable changed.");
        }

        public static string GetExecutable(OSType targetOS) => _lua_compilers_path[targetOS];
        public static string GetExecutable() => GetExecutable(GetOSSuffix());
        
        public static bool TryGetExecutable(OSType targetOS, [NotNullWhen(true)] out string? str)
            => _lua_compilers_path.TryGetValue(targetOS, out str);
        public static bool TryGetExecutable([NotNullWhen(true)] out string? str)
            => TryGetExecutable(GetOSSuffix(), out str);
    }
}
