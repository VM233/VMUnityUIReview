using System.Linq;
using NUnit.Framework;

namespace VMUnityAutomation.Editor.Tests
{
    public sealed class VmUiButtonFeedbackTests
    {
        [Test]
        public void NativeCascadeFeedbackAndRetirementControls()
        {
            var cases = VmUiButtonFeedbackSelfTests.Run().Concat(VmUiButtonContrastSelfTests.Run()).ToArray();
            Assert.That(cases.Length, Is.GreaterThanOrEqualTo(12));
            foreach (var result in cases)
                Assert.That(result["passed"], Is.True, MiniJson.Serialize(result));
        }
    }
}
