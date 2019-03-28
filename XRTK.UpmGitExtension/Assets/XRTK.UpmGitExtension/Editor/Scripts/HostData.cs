using System;
using UnityEngine;

namespace XRTK.PackageManager
{
    [Serializable]
    public class HostData
    {
        public string Name = "web";
        public string Domain = "undefined";
        public string Blob = "blob";
        public Texture2D LogoDark = null;
        public Texture2D LogoLight = null;
    }
}