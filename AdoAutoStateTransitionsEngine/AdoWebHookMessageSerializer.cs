using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AdoAutoStateTransitionsEngine
{
    public class AdoWebHookMessageSerializer
    {
        public AdoWebHookMessage LoadFile(string fileName)
        {
            using (var s = new StreamReader(fileName))
                return LoadFromReader(s);
        }

        public AdoWebHookMessage LoadFromString(string contents)
        {
            using (var s = new StringReader(contents))
                return LoadFromReader(s);
        }

        public AdoWebHookMessage LoadFromReader(TextReader reader)
        {
            using (var jr = new JsonTextReader(reader))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize<AdoWebHookMessage>(jr);
            }
        }
    }
}
