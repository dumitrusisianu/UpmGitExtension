using System.IO;
using Markdig;
using UnityEditor;
using UnityEngine;

namespace XRTK.PackageManager
{
    internal static class MarkdownUtilities
    {
        private const string k_CssFileName = "github-markdown";

        private static readonly MarkdownPipeline s_Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        private static readonly string s_TempDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp");

        public static void OpenInBrowser(string path)
        {
            string cssPath = Path.Combine(s_TempDir, k_CssFileName + ".css");

            if (!File.Exists(cssPath))
            {
                File.Copy(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(k_CssFileName)[0]), cssPath);
            }

            var htmlPath = Path.Combine(s_TempDir, $"{Path.GetFileNameWithoutExtension(path)}.html");

            using (var sr = new StreamReader(path))
            {
                using (var sw = new StreamWriter(htmlPath))
                {
                    sw.WriteLine($"<link rel=\"stylesheet\" type=\"text/css\" href=\"{k_CssFileName}.css\">");
                    sw.Write(Markdown.ToHtml(sr.ReadToEnd(), s_Pipeline));
                }
            }

            Application.OpenURL($"file://{htmlPath}");
        }
    }
}