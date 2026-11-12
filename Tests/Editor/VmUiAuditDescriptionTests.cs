using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace VMUnityAutomation.Editor.Tests
{
    public sealed class VmUiAuditDescriptionTests
    {

        [Test]
        public void UxmlLayoutAuditDescriptionPublishesTextOwnershipContract()
        {
            string description = VmUiReviewRouteProviderContractTests.Description(
                "uitoolkit/audit-uxml-layout");

            Assert.That(description, Does.Contain("empty labels"));
            Assert.That(description, Does.Contain("text binding"));
            Assert.That(description, Does.Contain("design-time preview"));
            Assert.That(description, Does.Contain("runtime text owner"));
            Assert.That(description,
                Does.Contain("non-overlapping absolute sibling stacks"));
            Assert.That(description,
                Does.Contain("single-axis ScrollViews fixed on their content-sized cross axis"));
            Assert.That(description,
                Does.Contain("fixed localized button widths"));
            Assert.That(description,
                Does.Contain("fixed content-only Label heights"));
            Assert.That(description,
                Does.Contain("layout-only balance, counterweight, spacer, or shim elements"));
        }

    }
}
