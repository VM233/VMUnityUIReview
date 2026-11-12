using System.Runtime.CompilerServices;
using VMUnityAutomation.Editor;

[assembly: VmProjectToolPackage("com.vm233.unity-ui-review")]
[assembly: InternalsVisibleTo("VMUnityUIReview.Editor.Tests")]
[assembly: VmAutomationBuiltInRouteProvider(typeof(VmUiReviewRouteProvider))]
