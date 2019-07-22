using AdoAutoStateTransitionsEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdoAutoStateTransitionsEngineTest
{
    [TestClass]
    public class MessageSerializerTests
    {
        [TestMethod]
        public void TestFileLoad()
        {
            var serializer = new AdoWebHookMessageSerializer();
            var message = serializer.LoadFile("TestMessage.json");

            Assert.IsNotNull(message);
            Assert.AreEqual(message.resource.workItemId, 336);
        }
    }
}
