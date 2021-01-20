using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Types.Relay;
using Microsoft.Extensions.Options;
using OrchardCore.ContentManagement.GraphQL.Options;

namespace OrchardCore.ContentManagement.GraphQL.Queries.Types
{
    public class ContentItemConnectionType : ConnectionType<ContentItemType>
    {
        public ContentItemConnectionType(IOptions<GraphQLContentOptions> optionsAccessor)
        { 
        }
    }
}
