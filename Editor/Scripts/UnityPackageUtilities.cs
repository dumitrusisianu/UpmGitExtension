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
            var match = Regex.Match(url, "(git@[^:]+):(.*)");
            var repoUrl = match.Success ? $"ssh://{match.Groups[1].Value}/{match.Groups[2].Value}" : url;
            return repoUrl.EndsWith(".git") ? repoUrl : $"{repoUrl}.git";
        }

        public static string GetRepoHttpUrl(PackageInfo packageInfo)
        {
            return GetRepoHttpUrl(packageInfo != null ? packageInfo.packageId : string.Empty);
        }

        public static string GetRepoHttpUrl(string packageId)
        {
            var match = Regex.Match(packageId, "^[^@]+@([^#]+)(#.+)?$");

            if (!match.Success) { return string.Empty; }

            var repoUrl = match.Groups[1].Value;
            repoUrl = Regex.Replace(repoUrl, "(git:)?git@([^:]+):", "https://$2/");
            repoUrl = repoUrl.Replace("ssh://", "https://");
            repoUrl = repoUrl.Replace("git@", string.Empty);
            repoUrl = Regex.Replace(repoUrl, "\\.git$", string.Empty);

            return repoUrl;

        }

        public static string GetRefName(string packageId)
        {
            var match = Regex.Match(packageId, "^[^@]+@[^#]+#(.+)$");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        public static string GetRepoId(string packageId)
        {
            var match = Regex.Match(GetRepoHttpUrl(packageId), "/([^/]+/[^/]+)$");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        public static string GetRevisionHash(PackageInfo packageInfo)
        {
            return GetRevisionHash(packageInfo != null ? packageInfo.resolvedPath : string.Empty);
        }

        private static string GetRevisionHash(string resolvedPath)
        {
            var match = Regex.Match(resolvedPath, "@([^@]+)$");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        public static string GetFileURL(PackageInfo packageInfo, string filePath)
        {
            return packageInfo != null
                ? GetFileURL(packageInfo.packageId, packageInfo.resolvedPath, filePath)
                : string.Empty;
        }

        private static string GetFileURL(string packageId, string resolvedPath, string filePath)
        {
            if (string.IsNullOrEmpty(filePath) ||
                string.IsNullOrEmpty(packageId) ||
                string.IsNullOrEmpty(resolvedPath))
            {
                return string.Empty;
            }

            var repoUrl = GetRepoHttpUrl(packageId);
            var hash = GetRevisionHash(resolvedPath);
            var blob = PackageManagerSettings.GetHostData(packageId).Blob;

            return $"{repoUrl}/{blob}/{hash}/{filePath}";
        }

        public static string GetFilePath(PackageInfo packageInfo, string filePattern)
        {
            return packageInfo != null
                ? GetFilePath(packageInfo.resolvedPath, filePattern)
                : string.Empty;
        }

        private static string GetFilePath(string resolvedPath, string filePattern)
        {
            if (string.IsNullOrEmpty(resolvedPath) || string.IsNullOrEmpty(filePattern))
            {
                return string.Empty;
            }

            foreach (var path in Directory.EnumerateFiles(resolvedPath, filePattern))
            {
                if (!path.EndsWith(".meta"))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        public static string GetSpecificPackageId(string packageId, string tag)
        {
            if (string.IsNullOrEmpty(packageId))
            {
                return string.Empty;
            }

            var match = Regex.Match(packageId, "^([^#]+)(#.+)?$");

            if (match.Success)
            {
                var id = match.Groups[1].Value;
                return string.IsNullOrEmpty(tag) ? id : $"{id}#{tag}";
            }

            return string.Empty;
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