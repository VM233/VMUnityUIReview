using System.Linq;
using NUnit.Framework;

namespace VMUnityAutomation.Editor.Tests
{
    public sealed class VmUiControlSkinTests
    {
        [Test]
        public void DefaultPartsAndActualStateProvenanceAreEnforced()
        {
            var cases = VmUiControlSkinSelfTests.Run().ToArray();
#if UNITY_6000_6_OR_NEWER
            Assert.That(cases.Length, Is.EqualTo(6));
#endif
            foreach (var result in cases)
                Assert.That(result["passed"], Is.True, MiniJson.Serialize(result));
        }
    }
}
