using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace XRTK.PackageManager
{
    internal static class UnityPackageUtilities
    {
        public static bool IsBusy => GitUtilities.IsGitRunning || (_request != null && _request.Status == StatusCode.InProgress);

        private static Request _request;
        private static Action<Request> _callback;

        public static string GetRepoUrl(string url)
        {
            Match m = Regex.Match(url, "(git@[^:]+):(.*)");
            string repoUrl = m.Success ? $"ssh://{m.Groups[1].Value}/{m.Groups[2].Value}" : url;
            return repoUrl.EndsWith(".git") ? repoUrl : $"{repoUrl}.git";
        }

        public static string GetRepoHttpUrl(PackageInfo packageInfo)
        {
            return GetRepoHttpUrl(packageInfo != null ? packageInfo.packageId : "");
        }

        public static string GetRepoHttpUrl(string packageId)
        {
            Match m = Regex.Match(packageId, "^[^@]+@([^#]+)(#.+)?$");

            if (!m.Success) { return ""; }

            var repoUrl = m.Groups[1].Value;
            repoUrl = Regex.Replace(repoUrl, "(git:)?git@([^:]+):", "https://$2/");
            repoUrl = repoUrl.Replace("ssh://", "https://");
            repoUrl = repoUrl.Replace("git@", "");
            repoUrl = Regex.Replace(repoUrl, "\\.git$", "");

            return repoUrl;

        }

        public static string GetRefName(string packageId)
        {
            Match m = Regex.Match(packageId, "^[^@]+@[^#]+#(.+)$");
            return m.Success ? m.Groups[1].Value : "";
        }

        public static string GetRepoId(string packageId)
        {
            Match m = Regex.Match(GetRepoHttpUrl(packageId), "/([^/]+/[^/]+)$");
            return m.Success ? m.Groups[1].Value : "";
        }

        public static string GetRevisionHash(PackageInfo packageInfo)
        {
            return GetRevisionHash(packageInfo != null ? packageInfo.resolvedPath : "");
        }

        private static string GetRevisionHash(string resolvedPath)
        {
            Match m = Regex.Match(resolvedPath, "@([^@]+)$");
            return m.Success ? m.Groups[1].Value : "";
        }

        public static string GetFileURL(PackageInfo packageInfo, string filePath)
        {
            return packageInfo != null
                ? GetFileURL(packageInfo.packageId, packageInfo.resolvedPath, filePath)
                : "";
        }

        private static string GetFileURL(string packageId, string resolvedPath, string filePath)
        {
            if (string.IsNullOrEmpty(filePath) ||
                string.IsNullOrEmpty(packageId) ||
                string.IsNullOrEmpty(resolvedPath))
            {
                return "";
            }

            string repoURL = GetRepoHttpUrl(packageId);
            string hash = GetRevisionHash(resolvedPath);
            string blob = PackageManagerSettings.GetHostData(packageId).Blob;

            return $"{repoURL}/{blob}/{hash}/{filePath}";
        }

        public static string GetFilePath(PackageInfo packageInfo, string filePattern)
        {
            return packageInfo != null
                ? GetFilePath(packageInfo.resolvedPath, filePattern)
                : "";
        }

        private static string GetFilePath(string resolvedPath, string filePattern)
        {
            if (string.IsNullOrEmpty(resolvedPath) || string.IsNullOrEmpty(filePattern))
            {
                return "";
            }

            foreach (var path in Directory.EnumerateFiles(resolvedPath, filePattern))
            {
                if (!path.EndsWith(".meta"))
                {
                    return path;
                }
            }

            return "";
        }

        public static string GetSpecificPackageId(string packageId, string tag)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                return "";
            }

            Match m = Regex.Match(packageId, "^([^#]+)(#.+)?$");

            if (m.Success)
            {
                var id = m.Groups[1].Value;
                return string.IsNullOrEmpty(tag) ? id : $"{id}#{tag}";
            }

            return "";
        }

        public static void AddPackage(string packageId, Action<Request> callback = null)
        {
            _request = Client.Add(packageId);
            _callback = callback;
            EditorUtility.DisplayProgressBar("Add Package", $"Cloning {packageId}", 0.5f);
            EditorApplication.update += UpdatePackageRequest;
        }

        private static void UpdatePackageRequest()
        {
            if (_request.Status != StatusCode.InProgress)
            {
                if (_request.Status == StatusCode.Failure)
                {
                    Debug.LogError($"Error: {_request.Error.message} ({_request.Error.errorCode})");
                }

                EditorApplication.update -= UpdatePackageRequest;
                EditorUtility.ClearProgressBar();
                _callback?.Invoke(_request);
                _request = null;
            }
        }
    }
}