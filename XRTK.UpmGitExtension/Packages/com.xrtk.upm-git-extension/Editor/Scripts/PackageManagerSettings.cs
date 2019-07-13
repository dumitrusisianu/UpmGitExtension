//using System.Linq;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.Serialization;
//
//namespace XRTK.PackageManager
//{
//    public class PackageManagerSettings : ScriptableObject
//    {
//        [SerializeField]
//        [FormerlySerializedAs("m_HostData")]
//        private HostData[] hostData = null;
//
//        public static HostData GetHostData(string packageId)
//        {
//            var settings = AssetDatabase.FindAssets($"t:{typeof(PackageManagerSettings).Name}")
//                .Select(AssetDatabase.GUIDToAssetPath)
//                .OrderBy(x => x)
//                .Select(AssetDatabase.LoadAssetAtPath<PackageManagerSettings>)
//                .FirstOrDefault();
//
//            Debug.Assert(settings != null);
//
//            return settings.hostData.FirstOrDefault(x => packageId.Contains(x.Domain))
//                ?? new HostData
//                {
//                    LogoDark = EditorGUIUtility.FindTexture("buildsettings.web.small"),
//                    LogoLight = EditorGUIUtility.FindTexture("d_buildsettings.web.small"),
//                };
//        }
//    }
//}