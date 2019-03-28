#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
#endif
using UnityEngine;
using System;
using System.IO;

namespace XRTK.PackageManager
{
    [Serializable]
    internal class PackageJsonHelper
    {
        [SerializeField]
        private string name = string.Empty;

        public static string GetPackageName(string path)
        {
            var jsonPath = Directory.Exists(path) ? Path.Combine(path, "package.json") : path;
            return File.Exists(jsonPath) && File.Exists($"{jsonPath}.meta")
                ? JsonUtility.FromJson<PackageJsonHelper>(File.ReadAllText(jsonPath)).name
                : "";
        }
    }
}
