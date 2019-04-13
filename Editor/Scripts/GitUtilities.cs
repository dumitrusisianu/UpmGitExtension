using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace XRTK.PackageManager
{
    internal static class GitUtilities
    {
        private const string k_Path = "Temp/UpmGit";

        public static bool IsGitRunning { get; private set; }

        private static readonly StringBuilder s_sbError = new StringBuilder();
        private static readonly StringBuilder s_sbOutput = new StringBuilder();
        private delegate void GitCommandCallback(bool success, string output);

        public static void GetRefs(string repoUrl, List<string> result, Action callback)
        {
            result.Clear();
            var args = $"ls-remote --refs -q {repoUrl}";

            void GitCommandCallback(bool success, string output)
            {
                if (success)
                {
                    foreach (Match m in Regex.Matches(output, "refs/(tags|heads)/(.*)$", RegexOptions.Multiline))
                    {
                        result.Add(m.Groups[2].Value.Trim());
                    }
                }

                callback();
            }

            ExecuteGitCommand(args, GitCommandCallback);
        }

        public static void GetPackageJson(string repoUrl, string branch, Action<string> onPackageFetch)
        {
            FileUtil.DeleteFileOrDirectory(k_Path);
            var args = $"clone --depth=1 --branch {branch} --single-branch {repoUrl} {k_Path}";
            ExecuteGitCommand(args, (_, __) => onPackageFetch(PackageJsonHelper.GetPackageName(k_Path)));
        }

        private static void ExecuteGitCommand(string args, GitCommandCallback gitCommandCallback)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                Arguments = args,
                CreateNoWindow = true,
                FileName = "git",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            var launchProcess = System.Diagnostics.Process.Start(startInfo);

            if (launchProcess == null || launchProcess.HasExited || launchProcess.Id == 0)
            {
                Debug.LogError("No 'git' executable was found. Please install Git on your system and restart Unity");
                gitCommandCallback(false, string.Empty);
            }
            else
            {
                //Add process callback.
                IsGitRunning = true;
                s_sbError.Length = 0;
                s_sbOutput.Length = 0;

                launchProcess.OutputDataReceived += (sender, e) => s_sbOutput.AppendLine(e.Data ?? "");
                launchProcess.ErrorDataReceived += (sender, e) => s_sbError.AppendLine(e.Data ?? "");
                launchProcess.Exited += OnLaunchProcessOnExited;
                launchProcess.BeginOutputReadLine();
                launchProcess.BeginErrorReadLine();
                launchProcess.EnableRaisingEvents = true;

                void OnLaunchProcessOnExited(object sender, EventArgs e)
                {
                    IsGitRunning = false;
                    bool success = 0 == launchProcess.ExitCode;

                    if (!success)
                    {
                        Debug.LogError($"Error: git {args}\n\n{s_sbError}");
                    }

                    gitCommandCallback(success, s_sbOutput.ToString());
                }
            }
        }
    }
}