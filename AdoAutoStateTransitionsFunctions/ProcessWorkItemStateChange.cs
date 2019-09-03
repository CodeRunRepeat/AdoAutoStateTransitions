using AdoAutoStateTransitionsEngine;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AdoAutoStateTransitionsFunctions
{
    public static class ProcessWorkItemStateChange
    {
        [FunctionName("ProcessWorkItemStateChange")]
        public static void Run(
            [ServiceBusTrigger("statechanges", "statechanges", Connection = "ServiceBusConnectionString")]string inputMessage, 
            ILogger log)
        {
            log.LogTrace($"ProcessWorkItemStateChange received: {inputMessage}");

            string adoOrganization = System.Environment.GetEnvironmentVariable("AdoUrl", EnvironmentVariableTarget.Process);
            log.LogTrace($"Connecting to ADO organization: {adoOrganization}");

            string pat = System.Environment.GetEnvironmentVariable("AdoPat", EnvironmentVariableTarget.Process);
            log.LogSensitive(LogLevel.Trace, "Using access token: {0}", pat);

            var adoEngine = new AdoEngine(adoOrganization, pat, log);

            var serializer = new AdoWebHookMessageSerializer();
            var message = serializer.LoadFromString(inputMessage);

            Task.WaitAll(
                adoEngine.UpdateActiveState(message), 
                adoEngine.UpdateClosedState(message),
                adoEngine.UpdateResolvedState(message));
        }

        private static void LogSensitive(this ILogger log, LogLevel logLevel, string format, params string[] values)
        {
            log.Log(logLevel, format, values.Select(v => Mask(v)).ToArray());
        }

        private static string Mask(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            const char maskChar = '*';
            if (s.Length < 4)
                return "".PadLeft(s.Length, maskChar);

            return string.Format("{0}{1}{2}", s[0], "".PadLeft(s.Length - 2, maskChar), s[s.Length - 1]);
        }
    }
}
