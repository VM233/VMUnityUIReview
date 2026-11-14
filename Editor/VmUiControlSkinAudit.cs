using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    internal sealed class VmUiControlSkinAudit
    {
        internal int Samples, Findings;
        private readonly HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
        private readonly VmUiFeedbackCoverage coverage;
#if UNITY_6000_6_OR_NEWER
        private readonly VmUiNativeStyleOrigin origins;
#endif

        internal VmUiControlSkinAudit(VmUiFeedbackCoverage coverage, StyleSheet theme = null, string themePath = null)
        {
            this.coverage = coverage;
#if UNITY_6000_6_OR_NEWER
            origins = new VmUiNativeStyleOrigin(coverage, theme, themePath);
#else
            throw new NotSupportedException("Native control-skin attribution requires Unity 6000.6 or newer.");
#endif
        }

        internal void RegisterTheme(StyleSheet theme, string path)
        {
#if UNITY_6000_6_OR_NEWER
            origins.RegisterTheme(theme, path);
#endif
        }

        internal void Inspect(VisualElement root, IReadOnlyList<VisualElement> nodes, string assetPath,
            string state, Action<VmAutomationUssStyleAuditIssue> record)
        {
#if UNITY_6000_6_OR_NEWER
            origins.BeginState();
            foreach (VisualElement node in nodes)
            {
                coverage.Samples++;
                Samples++;
                coverage.CheckBudget();
                IResolvedStyle style = node.resolvedStyle;
                if (style.backgroundColor.a > 0) Check("background-color", false);
                bool image = !style.backgroundImage.Equals(default(Background));
                if (image)
                {
                    Check("background-image", false);
                    Background background = style.backgroundImage;
                    UnityEngine.Object resource = background.sprite != null ? background.sprite :
                        background.texture != null ? background.texture :
                        background.vectorImage != null ? background.vectorImage : (UnityEngine.Object)background.renderTexture;
                    CheckResource(resource, "background-image");
                    if (style.unityBackgroundImageTintColor.a > 0 && style.unityBackgroundImageTintColor != Color.white)
                        Check("-unity-background-image-tint-color", false);
                }
                Vector4 widths = VmUiComputedBorderWidths.Read(node);
                if (widths.x > 0 && style.borderLeftColor.a > 0) Check("border-left-color", false);
                if (widths.y > 0 && style.borderTopColor.a > 0) Check("border-top-color", false);
                if (widths.z > 0 && style.borderRightColor.a > 0) Check("border-right-color", false);
                if (widths.w > 0 && style.borderBottomColor.a > 0) Check("border-bottom-color", false);
                if (node is TextElement text && !string.IsNullOrWhiteSpace(text.text))
                {
                    if (style.color.a > 0) Check("color", true);
                    FontDefinition font = style.unityFontDefinition;
                    if (font.font != null || font.fontAsset != null)
                    {
                        Check("-unity-font-definition", true);
                        CheckResource(font.font != null ? font.font : (UnityEngine.Object)font.fontAsset, "-unity-font-definition");
                    }
                    else if (style.unityFont != null)
                    {
                        Check("-unity-font", true);
                        CheckResource(style.unityFont, "-unity-font");
                    }
                }

                void Check(string property, bool inherited)
                {
                    string source = origins.Get(node, property, inherited);
                    if (!VmUiNativeStyleOrigin.IsUnity(source)) return;
                    Record(property, source);
                }

                void CheckResource(UnityEngine.Object resource, string property)
                {
                    string source = AssetDatabase.GetAssetPath(resource);
                    if (!string.IsNullOrEmpty(source) && !VmUiNativeStyleOrigin.IsProjectAsset(source))
                        Record(property, "Unity built-in resource (" + source + ")");
                }

                void Record(string property, string source)
                {
                    string path = VmAutomationUIToolkitElementUtility.GetElementPath(root, node);
                    if (!reported.Add(assetPath + "|" + path + "|" + state + "|" + property)) return;
                    Findings++;
                    record(new VmAutomationUssStyleAuditIssue
                    {
                        AssetPath = assetPath, Selector = path, Token = property,
                        Kind = "default-control-appearance", Severity = "error",
                        Message = "Player UI paint still comes from " + source + ": " + property +
                            ", state=" + state + ", native element=" + node.GetType().Name +
                            ". Skin the real generated part in the project's shared control family."
                    });
                }
            }
#endif
        }

        internal Dictionary<string, object> ToDictionary() => new Dictionary<string, object>
        {
            { "samples", Samples }, { "findings", Findings },
            { "scope", "NativePaintAndCurrentStateStyleProvenance" },
            { "manualArtReviewRequired", true }
        };
    }
}
