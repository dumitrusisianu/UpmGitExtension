﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace XRTK.PackageManager
{
    public class UpmGitAddWindow : EditorWindow
    {
        private bool _focused;
        private string _url = "";
        private string _repoUrl = "";
        private string _version = "(default)";
        private string _packageId = "";
        private readonly List<string> _refs = new List<string>();

        private GUIContent _errorUrl;
        private GUIContent _errorBranch;

        private void OnEnable()
        {
            _errorUrl = new GUIContent(EditorGUIUtility.FindTexture("console.erroricon.sml"), "Make sure you have the correct access rights and the repository exists.");
            _errorBranch = new GUIContent(EditorGUIUtility.FindTexture("console.erroricon.sml"), "package.json and package.json.meta do not exist in this branch/tag.");
            minSize = new Vector2(300, 40);
            maxSize = new Vector2(600, 40);
            titleContent = new GUIContent("Add package from URL");
        }

        private void PopupVersions(Action<string> onVersionChanged)
        {
            var menu = new GenericMenu();
            var currentRefName = _version;

            void Callback(object x) => onVersionChanged(x as string);

            // x.y(.z-sufix) only 
            foreach (var t in _refs.Where(x => Regex.IsMatch(x, "^\\d+\\.\\d+.*$")).OrderByDescending(x => x))
            {
                string target = t;
                bool isCurrent = currentRefName == target;
                var text = new GUIContent(target);
                menu.AddItem(text, isCurrent, Callback, target);
            }

            // other 
            menu.AddItem(new GUIContent("Other/(default)"), currentRefName == "", Callback, "(default)");

            foreach (var t in _refs.Where(x => !Regex.IsMatch(x, "^\\d+\\.\\d+.*$")).OrderByDescending(x => x))
            {
                string target = t;
                bool isCurrent = currentRefName == target;
                var text = new GUIContent($"Other/{target}");
                menu.AddItem(text, isCurrent, Callback, target);
            }

            menu.ShowAsContext();
        }

        private void OnGUI()
        {
            EditorGUIUtility.labelWidth = 100;

            using (new EditorGUI.DisabledScope(PackageUtilities.IsBusy))
            {
                using (var ccs = new EditorGUI.ChangeCheckScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUI.SetNextControlName("Repogitory URL");
                        _url = EditorGUILayout.TextField("Repogitory URL", _url);

                        if (!_focused)
                        {
                            EditorGUI.FocusTextInControl("Repogitory URL");
                            _focused = true;
                        }

                        if (ccs.changed)
                        {
                            _repoUrl = PackageUtilities.GetRepoUrl(_url);
                            _version = "-- Select Version --";
                            _packageId = "";
                            GitUtilities.GetRefs(_url, _refs, DelayedRepaint);
                        }

                        if (!PackageUtilities.IsBusy && !string.IsNullOrEmpty(_url) && _refs.Count == 0)
                        {
                            GUILayout.Label(_errorUrl, GUILayout.Width(20));
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Version");

                    using (new EditorGUI.DisabledScope(_refs.Count == 0))
                    {
                        if (GUILayout.Button(_version, EditorStyles.popup))
                        {
                            PopupVersions(OnVersionChanged);
                        }
                    }

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_packageId)))
                    {
                        if (GUILayout.Button(new GUIContent("Add", $"Add a package '{_packageId}' to the project."), EditorStyles.miniButton, GUILayout.Width(60)))
                        {
                            PackageUtilities.AddPackage(_packageId, CheckStatus);
                        }
                    }

                    if (_packageId == null)
                    {
                        GUILayout.Label(_errorBranch, GUILayout.Width(20));
                    }
                }
            }
        }

        private void OnVersionChanged(string ver)
        {
            _version = _refs.Contains(ver) ? ver : "HEAD";
            _packageId = "";
            GitUtilities.GetPackageJson(_url, _version, OnPackageFetch);
        }

        private void OnPackageFetch(string packageName)
        {
            _packageId = !string.IsNullOrEmpty(packageName)
                ? $"{packageName}@{_repoUrl}#{_version}"
                : null;

            EditorApplication.delayCall += DelayedRepaint;
        }

        private void CheckStatus(Request req)
        {
            if (req.Status == StatusCode.Success)
            {
                Close();
            }
        }

        private static void DelayedRepaint()
        {
            EditorApplication.delayCall += DelayedRepaint;
        }
    }
}