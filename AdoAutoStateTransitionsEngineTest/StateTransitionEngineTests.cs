using AdoAutoStateTransitionsEngine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace AdoAutoStateTransitionsEngineTests
{
    [TestClass]
    public class StateTransitionEngineTests
    {
        private AdoEngine engine;

        const int TestFeature1 = 2330;
        const int TestUserStory1 = 2327;
        const int TestUserStory2 = 2332;
        const int TestTask1 = 2328;
        const int TestTask2 = 2329;
        const int TestTask3 = 2331;
        const int TestBug1 = 2333;
        const int TestBug2 = 2334;

        [TestInitialize]
        public void LoadEngine()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json", true)
                .AddJsonFile("appSettings.json.user")
                .Build();

            var azureDevOpsOrganizationUrl = config["azureDevOpsOrganizationUrl"];
            var pat = config["pat"];

            using (var factory = new LoggerFactory(new ILoggerProvider[] { new DebugLoggerProvider() }))
            {
                engine = new AdoEngine(azureDevOpsOrganizationUrl, pat, factory.CreateLogger(""));
            }
        }

        [TestMethod]
        public void TestEngineConstructor()
        {
            Assert.IsNotNull(engine);
        }

        [TestMethod]
        public void TestGetWorkItem()
        {
            var wi = engine.GetWorkItem(TestFeature1).Result;
            Assert.IsNotNull(wi);
            Assert.AreEqual(TestFeature1, wi.Id.GetValueOrDefault());
        }

        [TestMethod]
        public void TestGetWorkItem2()
        {
            var wi = engine.GetWorkItem(TestFeature1.ToString()).Result;
            Assert.IsNotNull(wi);
            Assert.AreEqual(TestFeature1, wi.Id.GetValueOrDefault());
        }

        [TestMethod]
        public void TestGetParentWorkItem()
        {
            var wi = engine.GetParentWorkItem(TestUserStory1).Result;
            Assert.IsNotNull(wi);
            Assert.AreEqual(TestFeature1, wi.Id.GetValueOrDefault());
        }

        [TestMethod]
        public void TestGetChildrenWorkItems()
        {
            var children = engine.GetChildrenWorkItems(TestUserStory1);

            Assert.IsTrue(children.Count() >= 3);
            Assert.IsTrue(children.Select(c => c.Result).Any(r => r.Id == TestTask1));
        }

        [TestMethod]
        public void TestUpdateWorkItemState()
        {
            var wi = engine.GetWorkItem(TestFeature1).Result;
            var state = wi.GetState();

            var result = engine.UpdateWorkItemState(wi, state).Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(state, result.GetState());
        }

        [TestMethod]
        public void TestUpdateClosedState()
        {
            engine.UpdateWorkItemState(TestUserStory1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.Closed.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask2, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask2, WorkItemState.Closed.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask3, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask3, WorkItemState.Closed.ToString()).Wait();

            var message = GenerateUpdatedMessage(TestTask3, WorkItemState.Unknown, WorkItemState.Closed);
            engine.UpdateClosedState(message).Wait();

            var parent = engine.GetWorkItem(TestUserStory1).Result;
            Assert.AreEqual(WorkItemState.Closed.ToString(), parent.GetState());
        }

        [TestMethod]
        public void TestUpdateClosedState2()
        {
            engine.UpdateWorkItemState(TestUserStory1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.Closed.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask2, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask3, WorkItemState.New.ToString()).Wait();

            var message = GenerateUpdatedMessage(TestTask1, WorkItemState.Unknown, WorkItemState.Closed);
            engine.UpdateClosedState(message).Wait();

            var parent = engine.GetWorkItem(TestUserStory1).Result;
            Assert.AreEqual(WorkItemState.New.ToString(), parent.GetState());
        }

        [TestMethod]
        public void TestUpdateClosedState3()
        {
            engine.UpdateWorkItemState(TestUserStory1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.Removed.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask2, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask2, WorkItemState.Closed.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask3, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask3, WorkItemState.Closed.ToString()).Wait();

            var message = GenerateUpdatedMessage(TestTask1, WorkItemState.Unknown, WorkItemState.Removed);
            engine.UpdateClosedState(message).Wait();

            var parent = engine.GetWorkItem(TestUserStory1).Result;
            Assert.AreEqual(WorkItemState.Closed.ToString(), parent.GetState());
        }

        [TestMethod]
        public void TestUpdateClosedState4()
        {
            engine.UpdateWorkItemState(TestUserStory1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.Removed.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask2, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask2, WorkItemState.Removed.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask3, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask3, WorkItemState.Removed.ToString()).Wait();

            var message = GenerateUpdatedMessage(TestTask1, WorkItemState.Unknown, WorkItemState.Removed);
            engine.UpdateClosedState(message).Wait();

            var parent = engine.GetWorkItem(TestUserStory1).Result;
            Assert.AreEqual(WorkItemState.Removed.ToString(), parent.GetState());
        }

        [TestMethod]
        public void TestUpdateActiveState()
        {
            engine.UpdateWorkItemState(TestUserStory1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask1, WorkItemState.Active.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask2, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestTask3, WorkItemState.New.ToString()).Wait();

            var message = GenerateUpdatedMessage(TestTask1, WorkItemState.New, WorkItemState.Active);
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

        [TestMethod]
        public void TestUpdateResolvedState()
        {
            engine.UpdateWorkItemState(TestUserStory2, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestBug1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestBug1, WorkItemState.Resolved.ToString()).Wait();
            engine.UpdateWorkItemState(TestBug2, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestBug2, WorkItemState.Resolved.ToString()).Wait();

            var message = GenerateUpdatedMessage(TestBug1, WorkItemState.Unknown, WorkItemState.Resolved);
            engine.UpdateResolvedState(message).Wait();

            var parent = engine.GetWorkItem(TestUserStory2).Result;
            Assert.AreEqual(WorkItemState.Resolved.ToString(), parent.GetState());
        }

        [TestMethod]
        public void TestUpdateResolvedState2()
        {
            engine.UpdateWorkItemState(TestUserStory2, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestBug1, WorkItemState.New.ToString()).Wait();
            engine.UpdateWorkItemState(TestBug1, WorkItemState.Resolved.ToString()).Wait();
            engine.UpdateWorkItemState(TestBug2, WorkItemState.New.ToString()).Wait();

            var message = GenerateUpdatedMessage(TestBug1, WorkItemState.Unknown, WorkItemState.Resolved);
            engine.UpdateResolvedState(message).Wait();

            var parent = engine.GetWorkItem(TestUserStory2).Result;
            Assert.AreEqual(WorkItemState.New.ToString(), parent.GetState());
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
