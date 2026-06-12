using NUnit.Framework;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Unit tests for GameViewSizesBridge reflection layer.
    /// These tests verify that the Unity internal types are resolvable in the current Unity version
    /// and that the bridge returns coherent data.
    /// </summary>
    public class GameViewSizesBridgeTests
    {
        [Test]
        public void GetTotalCount_ReturnsPositiveValue()
        {
            int total = GameViewSizesBridge.GetTotalCount();
            Assert.That(total, Is.GreaterThan(0),
                "GetTotalCount should resolve GameViewSizes types and return at least one size");
        }

        [Test]
        public void GetDisplayTexts_ReturnsNonEmptyArray()
        {
            string[] texts = GameViewSizesBridge.GetDisplayTexts();
            Assert.That(texts, Is.Not.Null);
            Assert.That(texts.Length, Is.GreaterThan(0),
                "GetDisplayTexts should return at least one entry");
        }

        [Test]
        public void GetDisplayTexts_CountMatchesGetTotalCount()
        {
            string[] texts = GameViewSizesBridge.GetDisplayTexts();
            int total = GameViewSizesBridge.GetTotalCount();

            Assert.That(texts.Length, Is.EqualTo(total),
                "GetDisplayTexts length and GetTotalCount should agree");
        }

        [Test]
        public void FindSizeByLabel_FreeAspect_ReturnsZero()
        {
            // "Free Aspect" is always the first built-in size at index 0
            int index = GameViewSizesBridge.FindSizeByLabel("Free Aspect");
            Assert.That(index, Is.EqualTo(0),
                "Free Aspect should be at index 0");
        }

        [Test]
        public void FindSizeByLabel_UnknownLabel_ReturnsNegativeOne()
        {
            int index = GameViewSizesBridge.FindSizeByLabel("NONEXISTENT_LABEL_XYZ_99999");
            Assert.That(index, Is.EqualTo(-1),
                "Unknown label should return -1");
        }

        [Test]
        public void FindSizeByLabel_CaseInsensitive_FindsFreeAspect()
        {
            int lower = GameViewSizesBridge.FindSizeByLabel("free aspect");
            int upper = GameViewSizesBridge.FindSizeByLabel("FREE ASPECT");
            Assert.That(lower, Is.GreaterThanOrEqualTo(0),
                "FindSizeByLabel should be case-insensitive (lower)");
            Assert.That(upper, Is.GreaterThanOrEqualTo(0),
                "FindSizeByLabel should be case-insensitive (upper)");
            Assert.That(lower, Is.EqualTo(upper));
        }

        [Test]
        public void GetDisplayTexts_AllEntriesNonNull()
        {
            string[] texts = GameViewSizesBridge.GetDisplayTexts();
            foreach (string text in texts)
            {
                Assert.That(text, Is.Not.Null, "Each display text should be non-null");
            }
        }
    }
}
