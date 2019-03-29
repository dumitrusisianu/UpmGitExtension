using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System;
using System.Reflection;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif

namespace XRTK.PackageManager
{
    [InitializeOnLoad]
    internal class UpmGitExtensionUI : VisualElement, IPackageManagerExtension
    {
#if UPM_GIT_EXT_PROJECT
        private const string ResourcesPath = "Assets/XRTK.UpmGitExtension/Editor/Resources/";
#else
        const string ResourcesPath = "Packages/com.xrtk.upm-git-extension/Editor/Resources/";
#endif
        private const string TemplatePath = ResourcesPath + "UpmGitExtension.uxml";
        private const string StylePath = ResourcesPath + "UpmGitExtension.uss";

        private readonly List<string> _refs = new List<string>();

        private bool _initialized = false;
        private PackageInfo _packageInfo;
        private Button _hostingIcon => _gitDetailActions.Q<Button>("hostingIcon");
        private Button _viewDocumentation => _gitDetailActions.Q<Button>("viewDocumentation");
        private Button _viewChangelog => _gitDetailActions.Q<Button>("viewChangelog");
        private Button _viewLicense => _gitDetailActions.Q<Button>("viewLicense");
        private string _currentRefName => UnityPackageUtilities.GetRefName(_packageInfo.packageId);
        private string _selectedRefName => _versionPopup.text != "(default)" ? _versionPopup.text : "";
        private VisualElement _detailControls;
        private VisualElement _documentationContainer;
        private VisualElement _originalDetailActions;
        private VisualElement _gitDetailActions;
        private VisualElement _originalAddButton;
        private VisualElement _addButton;
        private Button _versionPopup;
        private Button _updateButton;
        private HostData _currentHostData = null;

        static UpmGitExtensionUI()
        {
            PackageManagerExtensions.RegisterExtension(new UpmGitExtensionUI());
        }

        #region IPackageManagerExtension Implementation

        /// <summary>
        /// Creates the extension UI visual element.
        /// </summary>
        /// <returns>A visual element that represents the UI or null if none</returns>
        VisualElement IPackageManagerExtension.CreateExtensionUI()
        {
            _initialized = false;
            return this;
        }

        /// <summary>
        /// Called by the Package Manager UI when a package is added or updated.
        /// </summary>
        /// <param name="packageInfo">The package information</param>
        void IPackageManagerExtension.OnPackageAddedOrUpdated(PackageInfo packageInfo)
        {
            _detailControls?.SetEnabled(true);
        }

        /// <summary>
        /// Called by the Package Manager UI when a package is removed.
        /// </summary>
        /// <param name="packageInfo">The package information</param>
        void IPackageManagerExtension.OnPackageRemoved(PackageInfo packageInfo)
        {
            _detailControls?.SetEnabled(true);
        }

        /// <summary>
        /// Called by the Package Manager UI when the package selection changed.
        /// </summary>
        /// <param name="packageInfo">The newly selected package information (can be null)</param>
        void IPackageManagerExtension.OnPackageSelectionChange(PackageInfo packageInfo)
        {
            InitializeUI();

            if (!_initialized ||
                packageInfo == null ||
                _packageInfo == packageInfo)
            {
                return;
            }

            _packageInfo = packageInfo;

            var isGit = packageInfo.source == PackageSource.Git;

            UIUtilities.SetElementDisplay(_gitDetailActions, isGit);
            UIUtilities.SetElementDisplay(_originalDetailActions, !isGit);
            UIUtilities.SetElementDisplay(_detailControls.Q("", "popupField"), !isGit);
            UIUtilities.SetElementDisplay(_updateButton, isGit);
            UIUtilities.SetElementDisplay(_versionPopup, isGit);
            UIUtilities.SetElementDisplay(_originalAddButton, false);
            UIUtilities.SetElementDisplay(_addButton, true);

            if (isGit)
            {
                _updateButton.text = "Update to";
                _versionPopup.SetEnabled(false);
                _updateButton.SetEnabled(false);

                GitUtilities.GetRefs(UnityPackageUtilities.GetRepoHttpUrl(_packageInfo.packageId), _refs, CheckCurrentRef);

                SetVersion(_currentRefName);

                EditorApplication.delayCall += DisplayDetailControls;

                _currentHostData = PackageManagerSettings.GetHostData(_packageInfo.packageId);

                _hostingIcon.tooltip = $"View on {_currentHostData.Name}";
                _hostingIcon.style.backgroundImage = EditorGUIUtility.isProSkin ? _currentHostData.LogoLight : _currentHostData.LogoDark;
            }
        }

        #endregion IPackageManagerExtension Implementation

        private void DisplayDetailControls()
        {
            UIUtilities.SetElementDisplay(_detailControls.Q("updateCombo"), true);
            UIUtilities.SetElementDisplay(_detailControls.Q("remove"), true);
            _detailControls.Q("remove").SetEnabled(true);
        }

        private void CheckCurrentRef()
        {
            _updateButton.SetEnabled(_currentRefName != _selectedRefName);
            _versionPopup.SetEnabled(true);
        }

        private void InitializeUI()
        {
            if (_initialized) { return; }

            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemplatePath);

            if (!asset) { return; }

#if UNITY_2019_1_OR_NEWER
            _gitDetailActoins = asset.CloneTree().Q("detailActions");
            _gitDetailActoins.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet> (StylePath));
#else
            _gitDetailActions = asset.CloneTree(null).Q("detailActions");
            _gitDetailActions.AddStyleSheetPath(StylePath);
#endif

            // Add callbacks
            _hostingIcon.clickable.clicked += () => Application.OpenURL(UnityPackageUtilities.GetRepoHttpUrl(_packageInfo));
            _viewDocumentation.clickable.clicked += () => MarkdownUtilities.OpenInBrowser(UnityPackageUtilities.GetFilePath(_packageInfo, "README.*"));
            _viewChangelog.clickable.clicked += () => MarkdownUtilities.OpenInBrowser(UnityPackageUtilities.GetFilePath(_packageInfo, "CHANGELOG.*"));
            _viewLicense.clickable.clicked += () => MarkdownUtilities.OpenInBrowser(UnityPackageUtilities.GetFilePath(_packageInfo, "LICENSE.*"));

            // Move element to documentationContainer
            _detailControls = parent.parent.Q("detailsControls") ?? parent.parent.parent.parent.Q("packageToolBar");
            _documentationContainer = parent.parent.Q("documentationContainer");
            _originalDetailActions = _documentationContainer.Q("detailActions");
            _documentationContainer.Add(_gitDetailActions);

            _updateButton = new Button(AddOrUpdatePackage) { name = "update", text = "Up to date" };
            _updateButton.AddToClassList("action");
            _versionPopup = new Button(PopupVersions);
            _versionPopup.AddToClassList("popup");
            _versionPopup.AddToClassList("popupField");
            _versionPopup.AddToClassList("versions");

            if (_detailControls.name == "packageToolBar")
            {
                _hostingIcon.style.borderLeftWidth = 0;
                _hostingIcon.style.borderRightWidth = 0;
                _versionPopup.style.marginLeft = -10;
                _detailControls.Q("rightItems").Insert(1, _updateButton);
                _detailControls.Q("rightItems").Insert(2, _versionPopup);
            }
            else
            {
                _versionPopup.style.marginLeft = -4;
                _versionPopup.style.marginRight = -3;
                _versionPopup.style.marginTop = -3;
                _versionPopup.style.marginBottom = -3;
                _detailControls.Q("updateCombo").Insert(1, _updateButton);
                _detailControls.Q("updateDropdownContainer").Add(_versionPopup);
            }

            // Add package button
            var root = UIUtilities.GetRoot(_gitDetailActions);
            _originalAddButton = root.Q("toolbarAddButton") ?? root.Q("moreAddOptionsButton");
            _addButton = new Button(AddPackage) { name = "moreAddOptionsButton", text = "+" };
            _addButton.AddToClassList("toolbarButton");
            _addButton.AddToClassList("space");
            _addButton.AddToClassList("pulldown");
            _originalAddButton.parent.Insert(_originalAddButton.parent.IndexOf(_originalAddButton) + 1, _addButton);
            _initialized = true;
        }

        private void PopupVersions()
        {
            var menu = new GenericMenu();
            var currentRefName = _currentRefName;

            menu.AddItem(new GUIContent($"{currentRefName} - current"), _selectedRefName == currentRefName, SetVersion, currentRefName);

            // x.y(.z-sufix) only 
            foreach (var t in _refs.Where(x => Regex.IsMatch(x, "^\\d+\\.\\d+.*$")).OrderByDescending(x => x))
            {
                string target = t;
                bool isCurrent = currentRefName == target;
                GUIContent text = new GUIContent($"All Versions/{(isCurrent ? $"{target} - current" : target)}");
                menu.AddItem(text, isCurrent, SetVersion, target);
            }

            // other 
            menu.AddItem(new GUIContent("All Versions/Other/(default)"), _selectedRefName == "", SetVersion, "(default)");

            foreach (var t in _refs.Where(x => !Regex.IsMatch(x, "^\\d+\\.\\d+.*$")).OrderByDescending(x => x))
            {
                string target = t;
                bool isCurrent = currentRefName == target;
                var text = new GUIContent($"All Versions/Other/{(isCurrent ? $"{target} - current" : target)}");
                menu.AddItem(text, isCurrent, SetVersion, target);
            }

            menu.DropDown(new Rect(_versionPopup.LocalToWorld(new Vector2(0, 10)), Vector2.zero));
        }

        private void SetVersion(object version)
        {
            var ver = version as string;
            _versionPopup.text = ver;
            _updateButton.SetEnabled(_currentRefName != _selectedRefName);
        }

        private void AddOrUpdatePackage()
        {
            var target = _versionPopup.text != "(default)" ? _versionPopup.text : "";
            var id = UnityPackageUtilities.GetSpecificPackageId(_packageInfo.packageId, target);
            UnityPackageUtilities.AddPackage(id);

            _versionPopup.SetEnabled(false);
            _updateButton.SetEnabled(false);
            _updateButton.text = "Updating to";
        }

        private void AddPackage()
        {
            var typePackage = Type.GetType("UnityEditor.PackageManager.UI.Package, Unity.PackageManagerUI.Editor");
            Debug.Assert(typePackage != null);
            var piAddRemoveOperationInProgress = typePackage.GetProperty("AddRemoveOperationInProgress", BindingFlags.Static | BindingFlags.Public);
            Debug.Assert(piAddRemoveOperationInProgress != null);
            var miAddFromLocalDisk = typePackage.GetMethod("AddFromLocalDisk", BindingFlags.Static | BindingFlags.NonPublic);
            Debug.Assert(miAddFromLocalDisk != null);

            var menu = new GenericMenu();

            var menuPosition = _addButton.LocalToWorld(new Vector2(_addButton.layout.width, 0));
            var menuRect = new Rect(menuPosition + new Vector2(0, -15), Vector2.zero);

            void AddPackageFromDiskWindow()
            {
                var path = EditorUtility.OpenFilePanelWithFilters("Select package on disk", "", new[] { "package.json file", "json" });

                if (!string.IsNullOrEmpty(path) &&
                    !(bool)piAddRemoveOperationInProgress.GetValue(null))
                {
                    miAddFromLocalDisk.Invoke(null, new object[] { path });
                }
            }

            menu.AddItem(new GUIContent("Add package from disk..."), false, AddPackageFromDiskWindow);
            menu.AddItem(new GUIContent("Add package from URL..."), false, ShowUpmGitAddWindow);
            menu.DropDown(menuRect);
        }

        private static void ShowUpmGitAddWindow()
        {
            EditorWindow.GetWindow<UpmGitAddWindow>(true);
        }
    }
}
