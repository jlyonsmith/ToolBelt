using System;
using NUnit.Framework;

namespace ServiceBelt.Tests
{
    [TestFixture]
    public class HiddenDataManagerTests
    {
        [Test]
        public void Tests()
        {
            var key = "JI9HtnRB9MK/ZUUpeO75HfPkjFMhnlYj3bvYJWAKLNc=";
            var manager = new HiddenDataManager(key);
            var originalText = "This is some text to hide";
            var hiddenText = manager.Hide(originalText);

            Assert.AreEqual(originalText, manager.Reveal(hiddenText));
        }
    }
}

