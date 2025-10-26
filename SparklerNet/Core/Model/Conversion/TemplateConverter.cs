using ProtoTemplate = SparklerNet.Core.Protobuf.Payload.Types.Template;

namespace SparklerNet.Core.Model.Conversion;

/// <summary>
///     Converts between <see cref="Template" /> and <see cref="ProtoTemplate" />.
/// </summary>
public static class TemplateConverter
{
    /// <summary>
    ///     Converts a <see cref="Template" /> to a Protobuf <see cref="ProtoTemplate" />.
    /// </summary>
    /// <param name="template">The Template to convert.</param>
    /// <returns>The converted Protobuf Template.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="template" /> is null.</exception>
    public static ProtoTemplate ToProtoTemplate(this Template template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var protoTemplate = new ProtoTemplate
        {
            IsDefinition = template.IsDefinition
        };

        // Set optional properties if they have values
        if (template.Version != null) protoTemplate.Version = template.Version;
        if (template.TemplateRef != null) protoTemplate.TemplateRef = template.TemplateRef;

        // Convert metrics using MetricConverter
        foreach (var metric in template.Metrics) protoTemplate.Metrics.Add(metric.ToProtoMetric());

        // Convert parameters using ParameterConverter
        // ReSharper disable once InvertIf
        if (template.Parameters != null)
            foreach (var parameter in template.Parameters)
                protoTemplate.Parameters.Add(parameter.ToProtoParameter());

        return protoTemplate;
    }

    /// <summary>
    ///     Converts a Protobuf <see cref="ProtoTemplate" /> to a <see cref="Template" />.
    /// </summary>
    /// <param name="protoTemplate">The Protobuf Template to convert.</param>
    /// <returns>The converted Template.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="protoTemplate" /> is null.</exception>
    public static Template ToTemplate(this ProtoTemplate protoTemplate)
    {
        ArgumentNullException.ThrowIfNull(protoTemplate);

        // Create a new Template with basic properties
        var template = new Template
        {
            Version = !string.IsNullOrEmpty(protoTemplate.Version) ? protoTemplate.Version : null,
            IsDefinition = protoTemplate.IsDefinition,
            TemplateRef = !string.IsNullOrEmpty(protoTemplate.TemplateRef) ? protoTemplate.TemplateRef : null
        };

        // Convert metrics using MetricConverter
        if (protoTemplate.Metrics.Count > 0)
            template.Metrics = protoTemplate.Metrics.Select(metric => metric.ToMetric()).ToList();

        // Convert parameters using ParameterConverter
        if (protoTemplate.Parameters.Count > 0)
            template.Parameters = protoTemplate.Parameters.Select(param => param.ToParameter()).ToList();

        return template;
    }
}