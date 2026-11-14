using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    /// <summary>One isolated native style context. No window, binding tick or input dispatch.</summary>
    internal sealed class VmUiFeedbackPanel : IDisposable
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type PanelType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.Panel", true);
        private static readonly Type PhaseType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.VisualTreeUpdatePhase", true);
        private static readonly PropertyInfo PseudoStates = typeof(VisualElement).GetProperty("pseudoStates", Members)
            ?? throw new MissingMemberException(typeof(VisualElement).FullName, "pseudoStates");
        private static readonly MethodInfo CreatePanel = PanelType.GetMethod("CreateEditorPanel",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
            new[] { typeof(ScriptableObject) }, null)
            ?? throw new MissingMethodException(PanelType.FullName, "CreateEditorPanel");
        private static readonly MethodInfo GetUpdater = PanelType.GetMethod("GetUpdater", Members,
            null, new[] { PhaseType }, null)
            ?? throw new MissingMethodException(PanelType.FullName, "GetUpdater");
        private static readonly object StylesPhase = Enum.Parse(PhaseType, "Styles");
        private static readonly long Hover = Convert.ToInt64(Enum.Parse(PseudoStates.PropertyType, "Hover"));
        private static readonly long Active = Convert.ToInt64(Enum.Parse(PseudoStates.PropertyType, "Active"));
        private static readonly long Focus = Convert.ToInt64(Enum.Parse(PseudoStates.PropertyType, "Focus"));
        private readonly List<VisualElement> stateChain = new List<VisualElement>();
        private ReviewOwner owner;
        private IDisposable panel;
        private ThemeStyleSheet theme;
        private object updater;
        private MethodInfo updateStyles;

        internal VisualElement Root { get; private set; }
        internal StyleSheet Theme => theme;

        internal VmUiFeedbackPanel(VisualElement content, ThemeStyleSheet sourceTheme)
        {
            if (content == null || sourceTheme == null)
                throw new ArgumentException("Native feedback review requires content and its actual theme.");
            try
            {
                owner = ScriptableObject.CreateInstance<ReviewOwner>();
                owner.hideFlags = HideFlags.HideAndDontSave;
                var native = CreatePanel.Invoke(null, new object[] { owner });
                panel = (IDisposable)native;
                Root = ((IPanel)native).visualTree;
                updater = GetUpdater.Invoke(native, new[] { StylesPhase });
                updateStyles = GetUpdater.ReturnType.GetMethod("Update", Members, null, Type.EmptyTypes, null)
                    ?? throw new MissingMethodException(GetUpdater.ReturnType.FullName, "Update");
                theme = UnityEngine.Object.Instantiate(sourceTheme);
                theme.hideFlags = HideFlags.HideAndDontSave;
                var defaultSheet = typeof(StyleSheet).GetProperty("isDefaultStyleSheet", Members)
                    ?? throw new MissingMemberException(typeof(StyleSheet).FullName, "isDefaultStyleSheet");
                defaultSheet.SetValue(theme, true);
                Root.styleSheets.Add(theme);
                Root.Add(content);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void SetState(VisualElement button, bool hovered, bool pressed, bool focused,
            VisualElement stateOwner = null)
        {
            ResetStates();
            stateOwner = stateOwner ?? button;
            int depth = 0;
            for (VisualElement node = button; node != null; node = node.hierarchy.parent)
            {
                if (++depth > VmUiFeedbackCoverage.MaximumDepth)
                    throw new InvalidOperationException("Native feedback ancestor depth exceeds 64.");
                long states = Convert.ToInt64(PseudoStates.GetValue(node)) & ~(Hover | Active | Focus);
                if (hovered) states |= Hover;
                if (node == stateOwner && pressed) states |= Active;
                if (node == stateOwner && focused) states |= Focus;
                PseudoStates.SetValue(node, Enum.ToObject(PseudoStates.PropertyType, states));
                stateChain.Add(node);
            }
            updateStyles.Invoke(updater, null);
        }

        private void ResetStates()
        {
            foreach (VisualElement node in stateChain)
            {
                long states = Convert.ToInt64(PseudoStates.GetValue(node)) & ~(Hover | Active | Focus);
                PseudoStates.SetValue(node, Enum.ToObject(PseudoStates.PropertyType, states));
            }
            stateChain.Clear();
        }

        public void Dispose()
        {
            try
            {
                ResetStates();
                panel?.Dispose();
            }
            finally
            {
                panel = null;
                Root = null;
                if (theme != null) UnityEngine.Object.DestroyImmediate(theme);
                if (owner != null) UnityEngine.Object.DestroyImmediate(owner);
                theme = null;
                owner = null;
            }
        }

        private sealed class ReviewOwner : ScriptableObject { }
    }
}
