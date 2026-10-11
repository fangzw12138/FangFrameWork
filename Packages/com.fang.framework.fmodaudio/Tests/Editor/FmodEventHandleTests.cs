using NUnit.Framework;

namespace Fang.Framework.FmodAudio.Editor.Tests
{
    public class FmodEventHandleTests
    {
        [Test]
        public void Invalid_is_not_valid_and_has_no_ids()
        {
            Assert.IsFalse(FmodEventHandle.Invalid.IsValid);
            Assert.AreEqual(0, FmodEventHandle.Invalid.InstanceId);
            Assert.AreEqual(0, FmodEventHandle.Invalid.ServiceGeneration);
        }

        [Test]
        public void A_handle_with_an_instance_id_is_valid()
        {
            Assert.IsTrue(new FmodEventHandle(1, 1).IsValid);
            Assert.IsTrue(new FmodEventHandle(int.MaxValue, 7).IsValid);
        }

        [Test]
        public void A_handle_without_an_instance_id_is_not_valid()
        {
            Assert.IsFalse(new FmodEventHandle(0, 4).IsValid);
        }

        [Test]
        public void A_handle_keeps_the_generation_it_was_created_with()
        {
            var handle = new FmodEventHandle(3, 12);

            Assert.AreEqual(3, handle.InstanceId);
            Assert.AreEqual(12, handle.ServiceGeneration);
        }

        [Test]
        public void Handles_of_the_same_instance_but_different_generations_differ()
        {
            var older = new FmodEventHandle(5, 1);
            var newer = new FmodEventHandle(5, 2);

            Assert.AreNotEqual(older, newer);
            Assert.AreEqual(older, new FmodEventHandle(5, 1));
        }
    }
}
