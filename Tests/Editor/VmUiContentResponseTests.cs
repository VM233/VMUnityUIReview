using System;
using NUnit.Framework;
using VMUnityAutomation.Runtime;

namespace VMUnityAutomation.Editor.Tests
{
    public sealed class VmUiContentResponseTests
    {
        [TestCase("这里的地面放不稳，换一块干燥、平整的地方吧")]
        [TestCase("Requirements not met")]
        public void RejectOriginalVagueReasons(string reason) =>
            Assert.That(VmUiContentAudit.ReasonIssue(reason), Is.EqualTo("vague-rejection-reason"));

        [TestCase("室内地板不能开垦农田，请到室外的泥土地面上开垦")]
        [TestCase("This patch overlaps water; choose dry ground")]
        public void AcceptConcreteCauses(string reason) => Assert.That(VmUiContentAudit.ReasonIssue(reason), Is.Null);

        [Test]
        public void RejectOriginalFlatDistrictBlock()
        {
            Assert.That(VmUiContentAudit.IsFlatBlock("城区\n种子与肥料\n地图格范围\n横向0～3\n范围内共享路障\n侦察附近街道\n路障0/3\n巡逻0分钟"), Is.True);
            Assert.That(VmUiContentAudit.IsFlatBlock("种子与肥料可能留在这里\n\n街区情况未知"), Is.False);
            Assert.That(VmUiContentAudit.IsFlatBlock("物品\n- 木材\n- 石块\n- 水\n- 食物"), Is.False);
            Assert.That(VmUiContentAudit.IsFlatBlock("<b>标题</b>\n城区\n种子与肥料\n地图格范围\n横向0～3\n范围内共享路障"), Is.True);
            Assert.That(VmUiContentAudit.IsFlatBlock("<b>街区</b>\n路障1\n巡逻30分钟\n战斗痕迹10%\n已清理2"), Is.False);
        }

        [Test]
        public void SeparatePublicationFromActuallyVisibleOnset()
        {
            var frames = new[] { Frame(1, 0, true, 0), Frame(2, .15, true, 0), Frame(3, .2, true, 1) };
            var delayed = VmUiTaskResponseAudit.Review(frames, 0, 100, 300);
            Assert.That(delayed.Findings, Does.Contain("delayed-visible-response"));
            Assert.That(delayed.Findings, Does.Not.Contain("delayed-publication"));
            frames[1] = Frame(2, .03, true, .1f);
            Assert.That(VmUiTaskResponseAudit.Review(frames, 0, 100, 300).Passed, Is.True);
            frames[0] = Frame(1, 0, false, 0); frames[1] = Frame(2, .15, false, 0);
            Assert.That(VmUiTaskResponseAudit.Review(frames, 0, 100, 300).Findings, Does.Contain("delayed-publication"));
        }

        [Test]
        public void RejectAResizeThatWaitsBehindAnOtherwisePromptFade()
        {
            var first = Frame(1, .02, true, .1f);
            var next = Frame(2, .18, true, .9f);
            var last = Frame(3, .25, true, 1); last.GeometryOffsetPixels = 12;
            var delayed = VmUiTaskResponseAudit.Review(new[] { first, next, last }, 0, 100, 350, true);
            Assert.That(delayed.Findings, Does.Contain("delayed-geometry-response"));
            Assert.That(delayed.Findings, Does.Not.Contain("delayed-visible-response"));
            first.GeometryOffsetPixels = 1;
            Assert.That(VmUiTaskResponseAudit.Review(new[] { first, next, last }, 0, 100, 350, true).Passed, Is.True);
        }

        [Test]
        public void RejectStaleAndAlternatingParentDestinations()
        {
            var visits = new[] { Visit("tools"), Visit("overview") };
            var bad = VmUiTaskResponseAudit.ReviewReturns(visits);
            Assert.That(bad.Findings, Does.Contain("wrong-return-destination"));
            Assert.That(bad.Findings, Does.Contain("inconsistent-return-destination"));
            visits[0] = Visit("overview");
            Assert.That(VmUiTaskResponseAudit.ReviewReturns(visits).Passed, Is.True);
        }

        [Test]
        public void RejectEmptyAndNonfiniteEvidence()
        {
            Assert.Throws<ArgumentException>(() => VmUiTaskResponseAudit.Review(Array.Empty<VmUiTaskResponseAudit.Frame>(), 0, 100, 300));
            Assert.Throws<ArgumentException>(() => VmUiTaskResponseAudit.Review(new[] { Frame(1, 0, true, 0), Frame(2, .1, true, float.NaN) }, 0, 100, 300));
            Assert.Throws<ArgumentException>(() => VmUiTaskResponseAudit.ReviewReturns(Array.Empty<VmUiTaskResponseAudit.ReturnVisit>()));
            var missing = VmUiTaskResponseAudit.Review(new[] { Frame(1, 0, false, 0), Frame(2, .1, false, 0) }, 0, 100, 300);
            Assert.That(missing.PublicationMilliseconds.HasValue, Is.False);
            Assert.That(missing.VisibleResponseMilliseconds.HasValue, Is.False);
        }

        private static VmUiTaskResponseAudit.Frame Frame(int number, double seconds, bool published, float painted) =>
            new VmUiTaskResponseAudit.Frame { Number = number, Seconds = seconds, Published = published, PaintedFraction = painted };
        private static VmUiTaskResponseAudit.ReturnVisit Visit(string actual) => new VmUiTaskResponseAudit.ReturnVisit
            { Context = "construction", Action = "back", ExpectedDestination = "overview", ActualDestination = actual };
    }
}
