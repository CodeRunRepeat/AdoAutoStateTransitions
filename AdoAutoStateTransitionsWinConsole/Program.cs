using AdoAutoStateTransitionsEngine;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace AdoAutoStateTransitionsWinConsole
{
    class Program
    {
        internal const string azureDevOpsOrganizationUrl = "https://dev.azure.com/csee2e";
        internal const string pat = "zf4ecmdtgslhhvrtnviek352kedbujkqx6gzk56b3nx3gl64bpia";

        static void Main(string[] args)
        {
            using (var s = new StreamReader("TestMessage.json"))
            using (var jr = new JsonTextReader(s))
            {
                var serializer = new JsonSerializer();
                var message = serializer.Deserialize<AdoWebHookMessage>(jr);

                var engine = new AdoEngine(azureDevOpsOrganizationUrl, pat);
                engine.UpdateActiveStates(message);
            }
            //var wi = engine.GetWorkItem(336);
            //wi.Wait();
            //Console.WriteLine(wi.Result.Url);

            ////wi = engine.GetParentWorkItem(336);
            ////wi.Wait();
            ////Console.WriteLine(wi.Result.Url);

            //wi = engine.UpdateWorkItemState(wi.Result, "Active");
            //wi.Wait();
            //Console.WriteLine(wi.Result.Fields["System.State"]);
        }
    }
}
