using System;
using NUnit.Framework;

namespace Fang.Framework.CommandConsole.Editor.Tests
{
    /// <summary><see cref="DebugCommandArgs"/> 的解析与错误文案（纯逻辑，不依赖场景）。</summary>
    public class DebugCommandArgsTests
    {
        [Test]
        public void Get_ReturnsRawValue()
        {
            var args = new DebugCommandArgs(new[] { "hello", "world" });

            Assert.AreEqual(2, args.Count);
            Assert.AreEqual("hello", args.Get(0, "text"));
            Assert.AreEqual("world", args.Get(1, "text"));
        }

        [Test]
        public void Constructor_NullArgs_CountIsZero()
        {
            var args = new DebugCommandArgs(null);

            Assert.AreEqual(0, args.Count);
        }

        [Test]
        public void Get_MissingIndex_ThrowsWithArgName()
        {
            var args = new DebugCommandArgs(Array.Empty<string>());

            var ex = Assert.Throws<DebugCommandException>(() => args.Get(0, "id"));

            StringAssert.Contains("id", ex.Message);
            StringAssert.Contains("缺少参数 1", ex.Message);
        }

        [Test]
        public void GetInt_ValidValue()
        {
            var args = new DebugCommandArgs(new[] { "42", "-7" });

            Assert.AreEqual(42, args.GetInt(0, "count"));
            Assert.AreEqual(-7, args.GetInt(1, "count"));
        }

        [Test]
        public void GetInt_InvalidValue_ThrowsWithIndexAndName()
        {
            var args = new DebugCommandArgs(new[] { "abc" });

            var ex = Assert.Throws<DebugCommandException>(() => args.GetInt(0, "count"));

            StringAssert.Contains("参数 1", ex.Message);
            StringAssert.Contains("count", ex.Message);
            StringAssert.Contains("abc", ex.Message);
        }

        [Test]
        public void GetFloat_ValidValue()
        {
            var args = new DebugCommandArgs(new[] { "1.5" });

            Assert.AreEqual(1.5f, args.GetFloat(0, "speed"), 0.0001f);
        }

        [Test]
        public void GetFloat_InvalidValue_Throws()
        {
            var args = new DebugCommandArgs(new[] { "fast" });

            Assert.Throws<DebugCommandException>(() => args.GetFloat(0, "speed"));
        }

        [Test]
        public void GetBool_AcceptsTrueFalse()
        {
            var args = new DebugCommandArgs(new[] { "true", "FALSE" });

            Assert.IsTrue(args.GetBool(0, "flag"));
            Assert.IsFalse(args.GetBool(1, "flag"));
        }

        [Test]
        public void GetBool_AcceptsOneZero()
        {
            var args = new DebugCommandArgs(new[] { "1", "0" });

            Assert.IsTrue(args.GetBool(0, "flag"));
            Assert.IsFalse(args.GetBool(1, "flag"));
        }

        [Test]
        public void GetBool_InvalidValue_Throws()
        {
            var args = new DebugCommandArgs(new[] { "yes" });

            Assert.Throws<DebugCommandException>(() => args.GetBool(0, "flag"));
        }

        [Test]
        public void GetVector3Int_ValidFormat()
        {
            var args = new DebugCommandArgs(new[] { "1,2,3" });

            var cell = args.GetVector3Int(0, "cell");

            Assert.AreEqual(1, cell.x);
            Assert.AreEqual(2, cell.y);
            Assert.AreEqual(3, cell.z);
        }

        [Test]
        public void GetVector3Int_InvalidFormat_Throws()
        {
            var args = new DebugCommandArgs(new[] { "1,2" });

            var ex = Assert.Throws<DebugCommandException>(() => args.GetVector3Int(0, "cell"));

            StringAssert.Contains("cell", ex.Message);
        }
    }
}
