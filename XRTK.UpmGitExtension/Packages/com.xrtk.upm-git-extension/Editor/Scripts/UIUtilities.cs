using UnityEngine.UIElements;

namespace XRTK.PackageManager
{
    internal static class UIUtilities
    {
        private const string k_DisplayNone = "display-none";

        public static void SetElementDisplay(VisualElement element, bool value)
        {
            if (element == null) { return; }

            SetElementClass(element, k_DisplayNone, !value);
            element.visible = value;
        }

        public static bool IsElementDisplay(VisualElement element)
        {
            return !HasElementClass(element, k_DisplayNone);
        }

        public static void SetElementClass(VisualElement element, string className, bool value)
        {
            if (element == null)
            {
                return;
            }

            if (value)
            {
                element.AddToClassList(className);
            }
            else
            {
                element.RemoveFromClassList(className);
            }
        }

        public static bool HasElementClass(VisualElement element, string className)
        {
            return element != null && element.ClassListContains(className);
        }

        public static VisualElement GetRoot(VisualElement element)
        {
            while (element?.parent != null)
            {
                element = element.parent;
            }

            return element;
        }
    }
}