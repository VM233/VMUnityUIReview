using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace VMUnityAutomation.Editor.Tests
{
    public sealed class VmUiReviewRouteProviderContractTests
    {
        [Test]
        public void MovedRoutesHaveOneExecutableOwnerAndClosedContracts()
        {
            var provider = new VmUiReviewRouteProvider();
            Assert.That(provider.Descriptors.Count, Is.EqualTo(12));
            VmAutomationBuiltInRouteProviderCatalog.ValidateManifest("UI review", provider.Descriptors,
                provider.AuditedRouteManifestSha256);
            foreach (var declared in provider.Descriptors)
            {
                var installed = Get(declared.Route);
                Assert.That(installed, Is.SameAs(declared));
                Assert.That(installed.InputSchema["additionalProperties"], Is.False);
                Assert.That(installed.OutputSchema, Is.Not.Empty);
                Assert.That(installed.Contract.ErrorCodes, Is.Not.Empty);
                Assert.That(VmAutomationBuiltInRouteDescriptorRegistry.Routes.Count(route => route == declared.Route), Is.EqualTo(1));
            }
        }

        internal static VmAutomationBuiltInRouteDescriptor Get(string route)
        {
            Assert.That(VmAutomationBuiltInRouteDescriptorRegistry.TryGet(route, out var descriptor), Is.True, route);
            Assert.That(descriptor.Package, Is.EqualTo("com.vm233.unity-ui-review"));
            Assert.That(descriptor.Contract, Is.Not.Null);
            return descriptor;
        }

        internal static bool TryGetOutput(string route, out Dictionary<string, object> output)
        {
            output = Get(route).OutputSchema;
            return true;
        }

        internal static string Description(string route) => Get(route).Description;
    }
}
