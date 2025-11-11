using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using ProtoTemplate = SparklerNet.Core.Protobuf.Payload.Types.Template;
using Payload = SparklerNet.Core.Protobuf.Payload;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class TemplateConverterTests
{
    [Theory]
    [InlineData("1.0.0", true, "device-template-v1", 25.5f, 60.0f, 60)]
    [InlineData("2.0.0", false, "device-template-v2", 30.0f, 70.0f, 30)]
    public void TemplateRoundTrip_PreservesData(string version, bool isDefinition, string templateRef, float tempValue,
        float humidValue, int intervalValue)
    {
        // Test that template data is preserved after round-trip conversion
        var originalTemplate = new Template
        {
            Version = version,
            IsDefinition = isDefinition,
            TemplateRef = templateRef,
            Metrics =
            [
                new Metric { Name = "temperature", DataType = DataType.Float, Value = tempValue },
                new Metric { Name = "humidity", DataType = DataType.Float, Value = humidValue }
            ],
            Parameters = [new Parameter { Name = "updateInterval", Type = DataType.Int32, Value = intervalValue }]
        };

        var protoTemplate = originalTemplate.ToProtoTemplate();
        var roundTripTemplate = protoTemplate.ToTemplate();

        Assert.NotNull(roundTripTemplate);
        Assert.Equal(originalTemplate.Version, roundTripTemplate.Version);
        Assert.Equal(originalTemplate.IsDefinition, roundTripTemplate.IsDefinition);
        Assert.Equal(originalTemplate.TemplateRef, roundTripTemplate.TemplateRef);
        Assert.Equal(originalTemplate.Metrics.Count, roundTripTemplate.Metrics.Count);
        Assert.NotNull(roundTripTemplate.Parameters);
        Assert.Equal(originalTemplate.Parameters.Count, roundTripTemplate.Parameters.Count);

        // Verify metrics
        Assert.NotNull(roundTripTemplate.Metrics);
        for (var i = 0; i < originalTemplate.Metrics.Count; i++)
        {
            Assert.Equal(originalTemplate.Metrics[i].Name, roundTripTemplate.Metrics[i].Name);
            Assert.Equal(originalTemplate.Metrics[i].DataType, roundTripTemplate.Metrics[i].DataType);
            Assert.Equal(originalTemplate.Metrics[i].Value, roundTripTemplate.Metrics[i].Value);
        }

        // Verify parameters
        for (var i = 0; i < originalTemplate.Parameters.Count; i++)
        {
            Assert.Equal(originalTemplate.Parameters[i].Name, roundTripTemplate.Parameters[i].Name);
            Assert.Equal(originalTemplate.Parameters[i].Type, roundTripTemplate.Parameters[i].Type);
            Assert.Equal(originalTemplate.Parameters[i].Value, roundTripTemplate.Parameters[i].Value);
        }
    }

    [Theory]
    [InlineData(null)]
    public void ToProtoTemplate_NullTemplate_ThrowsArgumentNullException(Template? template)
    {
        // Test that a null template throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => template!.ToProtoTemplate());
    }

    [Theory]
    [InlineData(null)]
    public void ToTemplate_NullProtoTemplate_ThrowsArgumentNullException(ProtoTemplate? protoTemplate)
    {
        // Test that a null proto template throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => protoTemplate!.ToTemplate());
    }

    [Theory]
    [InlineData("2.0", true, "basic-template")]
    [InlineData("1.5", false, "test-template")]
    public void ToProtoTemplate_BasicTemplate_ConvertsCorrectly(string version, bool isDefinition, string templateRef)
    {
        // Test basic template properties conversion to proto
        var template = new Template
        {
            Version = version,
            IsDefinition = isDefinition,
            TemplateRef = templateRef
        };

        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Equal(version, protoTemplate.Version);
        Assert.Equal(isDefinition, protoTemplate.IsDefinition);
        Assert.Equal(templateRef, protoTemplate.TemplateRef);
        Assert.Empty(protoTemplate.Metrics);
        Assert.Empty(protoTemplate.Parameters);
    }

    [Theory]
    [InlineData("3.0", false, "proto-template")]
    [InlineData("4.0", true, "another-template")]
    public void ToTemplate_BasicProtoTemplate_ConvertsCorrectly(string version, bool isDefinition, string templateRef)
    {
        // Test basic proto template properties conversion to template
        var protoTemplate = new ProtoTemplate
        {
            Version = version,
            IsDefinition = isDefinition,
            TemplateRef = templateRef
        };

        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.Equal(version, template.Version);
        Assert.Equal(isDefinition, template.IsDefinition);
        Assert.Equal(templateRef, template.TemplateRef);
        Assert.NotNull(template.Metrics);
        Assert.Empty(template.Metrics);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0")]
    public void ToProtoTemplate_TemplateWithMetrics_ConvertsMetricsCorrectly(string version)
    {
        // Test template metrics conversion to proto
        var template = new Template
        {
            Version = version,
            Metrics =
            [
                new Metric { Name = "pressure", DataType = DataType.Double, Value = 1013.25 },
                new Metric { Name = "status", DataType = DataType.Boolean, Value = true }
            ]
        };

        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Equal(2, protoTemplate.Metrics.Count);
        Assert.Contains(protoTemplate.Metrics, m => m.Name == "pressure" && Math.Abs(m.DoubleValue - 1013.25) < 0.001f);
        Assert.Contains(protoTemplate.Metrics, m => m.Name == "status" && m.BooleanValue);
    }

    [Theory]
    [InlineData(230.5f, 4.2f)]
    [InlineData(110.0f, 0.5f)]
    public void ToTemplate_ProtoTemplateWithMetrics_ConvertsMetricsCorrectly(float voltage, float current)
    {
        // Test proto template metrics conversion to template
        var protoTemplate = new ProtoTemplate();
        protoTemplate.Metrics.Add(new Payload.Types.Metric
            { Name = "voltage", Datatype = (uint)DataType.Float, FloatValue = voltage });
        protoTemplate.Metrics.Add(new Payload.Types.Metric
            { Name = "current", Datatype = (uint)DataType.Float, FloatValue = current });

        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.Equal(2, template.Metrics.Count);
        Assert.Contains(template.Metrics,
            m => m is { Name: "voltage", Value: float fv } && Math.Abs(fv - voltage) < 0.001f);
        Assert.Contains(template.Metrics,
            m => m is { Name: "current", Value: float fc } && Math.Abs(fc - current) < 0.001f);
    }

    [Theory]
    [InlineData("1.5", 100, "automatic")]
    [InlineData("2.0", 200, "manual")]
    public void ToProtoTemplate_TemplateWithParameters_ConvertsParametersCorrectly(string version, int thresholdValue,
        string modeValue)
    {
        // Test template parameters conversion to proto
        var template = new Template
        {
            Version = version,
            Parameters =
            [
                new Parameter { Name = "threshold", Type = DataType.Int32, Value = thresholdValue },
                new Parameter { Name = "mode", Type = DataType.String, Value = modeValue }
            ]
        };

        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Equal(2, protoTemplate.Parameters.Count);
        Assert.Contains(protoTemplate.Parameters, p => p.Name == "threshold" && p.IntValue == thresholdValue);
        Assert.Contains(protoTemplate.Parameters, p => p.Name == "mode" && p.StringValue == modeValue);
    }

    [Theory]
    [InlineData(30, "seconds")]
    [InlineData(60, "minutes")]
    public void ToTemplate_ProtoTemplateWithParameters_ConvertsParametersCorrectly(int timeoutValue, string unitValue)
    {
        // Test proto template parameters conversion to template
        var protoTemplate = new ProtoTemplate();
        protoTemplate.Parameters.Add(new ProtoTemplate.Types.Parameter
            { Name = "timeout", Type = (uint)DataType.Int32, IntValue = (uint)timeoutValue });
        protoTemplate.Parameters.Add(new ProtoTemplate.Types.Parameter
            { Name = "unit", Type = (uint)DataType.String, StringValue = unitValue });

        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.NotNull(template.Parameters);
        Assert.Equal(2, template.Parameters.Count);
        Assert.Contains(template.Parameters, p => p is { Name: "timeout", Value: int value } && value == timeoutValue);
        Assert.Contains(template.Parameters, p => p.Name == "unit" && p.Value as string == unitValue);
    }

    [Fact]
    public void ToProtoTemplate_EmptyTemplate_ReturnsEmptyProtoTemplate()
    {
        // Test empty template conversion to proto
        var template = new Template();
        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Equal(string.Empty, protoTemplate.Version);
        Assert.False(protoTemplate.IsDefinition);
        Assert.Equal(string.Empty, protoTemplate.TemplateRef);
        Assert.Empty(protoTemplate.Metrics);
        Assert.Empty(protoTemplate.Parameters);
    }

    [Fact]
    public void ToTemplate_EmptyProtoTemplate_ReturnsTemplateWithDefaultValues()
    {
        // Test empty proto template conversion to template
        var protoTemplate = new ProtoTemplate();
        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.Null(template.Version);
        Assert.False(template.IsDefinition);
        Assert.Null(template.TemplateRef);
        Assert.NotNull(template.Metrics);
        Assert.Empty(template.Metrics);
    }

    [Fact]
    public void ToProtoTemplate_NullMetrics_ReturnsEmptyMetrics()
    {
        // Test null metrics conversion to proto
        var template = new Template { Metrics = null! };
        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Empty(protoTemplate.Metrics);
    }

    [Fact]
    public void ToProtoTemplate_NullParameters_ReturnsEmptyParameters()
    {
        // Test null parameters conversion to proto
        var template = new Template { Parameters = null };
        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Empty(protoTemplate.Parameters);
    }

    [Theory]
    [InlineData("")]
    public void ToTemplate_ZeroLengthVersion_ReturnsNullVersion(string emptyString)
    {
        // Test empty string version converts to null
        var protoTemplate = new ProtoTemplate { Version = emptyString };
        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.Null(template.Version);
    }

    [Theory]
    [InlineData("")]
    public void ToTemplate_ZeroLengthTemplateRef_ReturnsNullTemplateRef(string emptyString)
    {
        // Test empty string template ref converts to null
        var protoTemplate = new ProtoTemplate { TemplateRef = emptyString };
        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.Null(template.TemplateRef);
    }
}