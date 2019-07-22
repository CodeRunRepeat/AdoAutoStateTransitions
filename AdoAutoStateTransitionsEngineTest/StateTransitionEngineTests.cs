using AdoAutoStateTransitionsEngine;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace AdoAutoStateTransitionsEngineTests
{
    [TestClass]
    public class StateTransitionEngineTests
    {
        private AdoEngine engine;

        [TestInitialize]
        public void LoadEngine()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", true)
                .AddJsonFile("appSettings.json.user")
                .Build();

            var azureDevOpsOrganizationUrl = config["azureDevOpsOrganizationUrl"];
            var pat = config["pat"];

            engine = new AdoEngine(azureDevOpsOrganizationUrl, pat);
        }

        [TestMethod]
        public void TestEngineConstructor()
        {
            Assert.IsNotNull(engine);
        }

        [TestMethod]
        public void TestGetWorkItem()
        {
            var wi = engine.GetWorkItem(336).Result;
            Assert.IsNotNull(wi);
            Assert.AreEqual(336, wi.Id.GetValueOrDefault());
        }

        [TestMethod]
        public void TestGetWorkItem2()
        {
            var wi = engine.GetWorkItem("336").Result;
            Assert.IsNotNull(wi);
            Assert.AreEqual(336, wi.Id.GetValueOrDefault());
        }

        [TestMethod]
        public void TestGetParentWorkItem()
        {
            var wi = engine.GetParentWorkItem(336).Result;
            Assert.IsNotNull(wi);
            Assert.AreEqual(306, wi.Id.GetValueOrDefault());
        }

        [TestMethod]
        public void TestGetChildrenWorkItems()
        {
            var children = engine.GetChildrenWorkItems(240);

            Assert.IsTrue(children.Count() >= 8);
            Assert.IsTrue(children.Select(c => c.Result).Any(r => r.Id == 306));
        }

        [TestMethod]
        public void TestUpdateWorkItemState()
        {
            var wi = engine.GetWorkItem("336").Result;
            var state = wi.GetState();

            var result = engine.UpdateWorkItemState(wi, state).Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(state, result.GetState());
        }

        [TestMethod]
        public void UpdateClosedState()
        {
            engine.UpdateWorkItemState(337, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(338, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(338, WorkItemState.Closed.ToString()).Wait();
            engine.UpdateWorkItemState(339, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(339, WorkItemState.Closed.ToString()).Wait();

            var message = GenerateUpdatedMessage(338, WorkItemState.Unknown, WorkItemState.Closed);
            engine.UpdateClosedState(message).Wait();

            var parent = engine.GetWorkItem(337).Result;
            Assert.AreEqual(WorkItemState.Closed.ToString(), parent.GetState());
        }

        [TestMethod]
        public void UpdateClosedState2()
        {
            engine.UpdateWorkItemState(337, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(338, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(338, WorkItemState.Closed.ToString()).Wait();
            engine.UpdateWorkItemState(339, WorkItemState.New.ToString()).Wait();

            var message = GenerateUpdatedMessage(338, WorkItemState.Unknown, WorkItemState.Closed);
            engine.UpdateClosedState(message).Wait();

            var parent = engine.GetWorkItem(337).Result;
            Assert.AreEqual(WorkItemState.New.ToString(), parent.GetState());
        }

        [TestMethod]
        public void UpdateClosedState3()
        {
            engine.UpdateWorkItemState(337, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(338, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(338, WorkItemState.Removed.ToString()).Wait();
            engine.UpdateWorkItemState(339, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(339, WorkItemState.Closed.ToString()).Wait();

            var message = GenerateUpdatedMessage(338, WorkItemState.Unknown, WorkItemState.Removed);
            engine.UpdateClosedState(message).Wait();

            var parent = engine.GetWorkItem(337).Result;
            Assert.AreEqual(WorkItemState.Closed.ToString(), parent.GetState());
        }

        [TestMethod]
        public void UpdateClosedState4()
        {
            engine.UpdateWorkItemState(337, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(338, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(338, WorkItemState.Removed.ToString()).Wait();
            engine.UpdateWorkItemState(339, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(339, WorkItemState.Removed.ToString()).Wait();

            var message = GenerateUpdatedMessage(338, WorkItemState.Unknown, WorkItemState.Removed);
            engine.UpdateClosedState(message).Wait();

            var parent = engine.GetWorkItem(337).Result;
            Assert.AreEqual(WorkItemState.Removed.ToString(), parent.GetState());
        }

        [TestMethod]
        public void TestUpdateActiveState()
        {
            AdoWebHookMessage message = GenerateUpdatedMessage(336, WorkItemState.New, WorkItemState.Active);
            engine.UpdateActiveState(message).Wait();

            var id = message.resource.workItemId;
            while (id > 0)
            {
                var parent = engine.GetParentWorkItem(id).Result;
                if (parent != null)
                {
                    Assert.AreNotEqual("New", parent.GetState());
                    id = parent.Id.GetValueOrDefault();
                }
                else
                    id = 0;
            }
        }

        private static AdoWebHookMessage GenerateUpdatedMessage(int workItemId, WorkItemState oldState, WorkItemState active)
        {
            return new AdoWebHookMessage()
            {
                eventType = "workitem.updated",
                resource = new Resource()
                {
                    workItemId = workItemId,
                    fields = new Fields()
                    {
                        SystemState = new StringChange()
                        {
                            oldValue = oldState.ToString(),
                            newValue = active.ToString(),
                        }
                    }
                }
            };
        }
    }
}
