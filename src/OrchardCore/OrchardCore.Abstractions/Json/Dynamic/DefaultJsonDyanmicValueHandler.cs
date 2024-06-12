using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;

namespace OrchardCore.Json.Dynamic;
public class DefaultJsonDyanmicValueHandler : IJsonDynamicValueHandler
{
    public bool GetValue(JsonNode parentNode, JsonNode currentNode, string nodeName, out object result)
    {
        if (currentNode is JsonValue jsonValue)
        {
            var valueKind = jsonValue.GetValueKind();
            switch (valueKind)
            {
                case JsonValueKind.String:
                    if (nodeName == "Value")
                    {
                        if (jsonValue.TryGetValue<DateTime>(out var datetime))
                        {
                            result = datetime;
                            return true;
                        }

                        if (jsonValue.TryGetValue<TimeSpan>(out var timeSpan))
                        {
                            result = timeSpan;
                            return true;
                        }
                    }
                    result = jsonValue.GetString();
                    return true;
                case JsonValueKind.Number:
                    result = jsonValue.GetNumber();
                    return true;
                case JsonValueKind.True:
                    result = true;
                    return true;
                case JsonValueKind.False:
                    result = false;
                    return true;
            }
        }
        result = null;
        return false;
    }
}
