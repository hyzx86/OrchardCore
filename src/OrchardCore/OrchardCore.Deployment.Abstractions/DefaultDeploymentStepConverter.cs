using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrchardCore.Deployment;

public class DefaultDeploymentStepConverter : JsonConverter<DeploymentStep>
{
    public override DeploymentStep Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "name":
                        var stepName = reader.GetString();
                        throw new InvalidCastException(string.Format(@"The specified deployment step [{0}] has not been registered.
You should use code similar to the following to register your custom step:
`services.AddDeployment<AdminMenuDeploymentSource, AdminMenuDeploymentStep, AdminMenuDeploymentStepDriver>();`
for more details, please refer to: https://docs.orchardcore.net/en/latest/docs/releases/1.9.0/#drop-newtonsoftjson-support", stepName));
                }
            }
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DeploymentStep value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
