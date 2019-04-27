using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace XRTK.PackageManager.Tests
{
    public class PackageManagerTests
    {
        private const string packageName = "com.xrtk.upm-git-extension";
        private const string userRepo = "XRTK/GitPackageTest";
        private const string repoURL = "https://github.com/" + userRepo;

        [TestCase("", ExpectedResult = "")]
        [TestCase(packageName + "@https://github.com/" + userRepo + ".git", ExpectedResult = repoURL)]
        [TestCase(packageName + "@https://github.com/" + userRepo + ".git#0.3.0", ExpectedResult = repoURL)]
        [TestCase(packageName + "@ssh://git@github.com/" + userRepo + ".git", ExpectedResult = repoURL)]
        [TestCase(packageName + "@ssh://git@github.com/" + userRepo + ".git#0.3.0", ExpectedResult = repoURL)]
        [TestCase(packageName + "@git@github.com:" + userRepo + ".git", ExpectedResult = repoURL)]
        [TestCase(packageName + "@git@github.com:" + userRepo + ".git#0.3.0", ExpectedResult = repoURL)]
        [TestCase(packageName + "@git:git@github.com:" + userRepo + ".git", ExpectedResult = repoURL)]
        [TestCase(packageName + "@git:git@github.com:" + userRepo + ".git#0.3.0", ExpectedResult = repoURL)]
        public string GetRepoURLTest(string packageId)
        {
            return UnityPackageUtilities.GetRepoHttpUrl(packageId);
        }

        [TestCase("", ExpectedResult = "")]
        [TestCase(packageName + "@https://github.com/" + userRepo + ".git", ExpectedResult = userRepo)]
        [TestCase(packageName + "@https://github.com/" + userRepo + ".git#0.3.0", ExpectedResult = userRepo)]
        [TestCase(packageName + "@ssh://git@github.com/" + userRepo + ".git", ExpectedResult = userRepo)]
        [TestCase(packageName + "@ssh://git@github.com/" + userRepo + ".git#0.3.0", ExpectedResult = userRepo)]
        [TestCase(packageName + "@git@github.com:" + userRepo + ".git", ExpectedResult = userRepo)]
        [TestCase(packageName + "@git@github.com:" + userRepo + ".git#0.3.0", ExpectedResult = userRepo)]
        [TestCase(packageName + "@git:git@github.com:" + userRepo + ".git", ExpectedResult = userRepo)]
        [TestCase(packageName + "@git:git@github.com:" + userRepo + ".git#0.3.0", ExpectedResult = userRepo)]
        public string GetRepoIdTest(string packageId)
        {
            return UnityPackageUtilities.GetRepoId(packageId);
        }

        [TestCase("", ExpectedResult = true)]
        [TestCase("true", ExpectedResult = true)]
        [TestCase("false", ExpectedResult = false)]
        [TestCase("false,true", ExpectedResult = true)]
        [TestCase("true,false", ExpectedResult = false)]
        public bool ElementVisibleTest(string operations)
        {
            var element = new VisualElement();

            if (0 < operations.Length)
            {
                foreach (bool flag in operations.Split(',').Select(System.Convert.ToBoolean))
                {
                    UIUtilities.SetElementDisplay(element, flag);
                }
            }

            return UIUtilities.IsElementDisplay(element);
        }

        [TestCase("", ExpectedResult = false)]
        [TestCase("true", ExpectedResult = true)]
        [TestCase("false", ExpectedResult = false)]
        [TestCase("false,true", ExpectedResult = true)]
        [TestCase("true,false", ExpectedResult = false)]
        public bool ElementClassTest(string operations)
        {
            var element = new VisualElement();

            if (0 < operations.Length)
            {
                foreach (bool flag in operations.Split(',').Select(System.Convert.ToBoolean))
                {
                    UIUtilities.SetElementClass(element, "test", flag);
                }
            }

            return UIUtilities.HasElementClass(element, "test");
        }
    }
}
