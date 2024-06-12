using System.Collections.Generic;
using System.Text.Json.Dynamic;
using System.Text.Json.Nodes;

namespace OrchardCore.Json.Dynamic;
public interface IJsonDynamicValueHandler
{
    /// <summary>
    /// Building dynamic fetch logic for the ContentItem.Content property.
    /// <para><see cref="JsonDynamicObject.GetValue(string)"/></para>
    /// <para><seealso cref="DefaultJsonDyanmicValueHandler"/></para>
    /// </summary>
    /// <param name="parentNode"></param>
    /// <param name="currentNode"></param>
    /// <param name="nodeName"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    bool GetValue(JsonNode parentNode, JsonNode currentNode, string nodeName, out object result);
}
