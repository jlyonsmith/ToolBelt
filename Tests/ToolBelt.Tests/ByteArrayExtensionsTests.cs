using System;
using NUnit.Framework;

namespace ToolBelt.Tests
{
    [TestFixture]
    public class ByteArrayExtensionsTests
    {
        [Test]
        public void ByteArrayToHexString()
        {
            var bytes1 = new byte[] { 0xfe, 0xdc, 0xba, 0x98, 0x76, 0x54, 0x32, 0x10 };
            var bytes2 = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };

            Assert.AreEqual("fedcba9876543210", bytes1.ToHex());
            Assert.AreEqual("0123456789abcdef", bytes2.ToHex());
        }
    }
}

