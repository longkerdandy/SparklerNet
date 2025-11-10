using SparklerNet.Core.Model;
using SparklerNet.Core.Model.Conversion;
using Xunit;
using ProtoTemplate = SparklerNet.Core.Protobuf.Payload.Types.Template;
using Payload = SparklerNet.Core.Protobuf.Payload;

namespace SparklerNet.Tests.Core.Model.Conversion;

public class TemplateConverterTests
{
    [Fact]
    public void TemplateRoundTrip_PreservesData()
    {
        var originalTemplate = new Template
        {
            Version = "1.0.0",
            IsDefinition = true,
            TemplateRef = "device-template-v1",
            Metrics =
            [
                new Metric { Name = "temperature", DataType = DataType.Float, Value = 25.5f },
                new Metric { Name = "humidity", DataType = DataType.Float, Value = 60.0f }
            ],
            Parameters = [new Parameter { Name = "updateInterval", Type = DataType.Int32, Value = 60 }]
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

    [Fact]
    public void ToProtoTemplate_NullTemplate_ThrowsArgumentNullException()
    {
        Template template = null!;
        Assert.Throws<ArgumentNullException>(() => template.ToProtoTemplate());
    }

    [Fact]
    public void ToTemplate_NullProtoTemplate_ThrowsArgumentNullException()
    {
        ProtoTemplate protoTemplate = null!;
        Assert.Throws<ArgumentNullException>(() => protoTemplate.ToTemplate());
    }

    [Fact]
    public void ToProtoTemplate_BasicTemplate_ConvertsCorrectly()
    {
        var template = new Template
        {
            Version = "2.0",
            IsDefinition = true,
            TemplateRef = "basic-template"
        };

        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Equal("2.0", protoTemplate.Version);
        Assert.True(protoTemplate.IsDefinition);
        Assert.Equal("basic-template", protoTemplate.TemplateRef);
        Assert.Empty(protoTemplate.Metrics);
        Assert.Empty(protoTemplate.Parameters);
    }

    [Fact]
    public void ToTemplate_BasicProtoTemplate_ConvertsCorrectly()
    {
        var protoTemplate = new ProtoTemplate
        {
            Version = "3.0",
            IsDefinition = false,
            TemplateRef = "proto-template"
        };

        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.Equal("3.0", template.Version);
        Assert.False(template.IsDefinition);
        Assert.Equal("proto-template", template.TemplateRef);
        Assert.NotNull(template.Metrics);
        Assert.Empty(template.Metrics);
    }

    [Fact]
    public void ToProtoTemplate_TemplateWithMetrics_ConvertsMetricsCorrectly()
    {
        var template = new Template
        {
            Version = "1.0",
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

    [Fact]
    public void ToTemplate_ProtoTemplateWithMetrics_ConvertsMetricsCorrectly()
    {
        var protoTemplate = new ProtoTemplate();
        protoTemplate.Metrics.Add(new Payload.Types.Metric
            { Name = "voltage", Datatype = (uint)DataType.Float, FloatValue = 230.5f });
        protoTemplate.Metrics.Add(new Payload.Types.Metric
            { Name = "current", Datatype = (uint)DataType.Float, FloatValue = 4.2f });

        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.Equal(2, template.Metrics.Count);
        Assert.Contains(template.Metrics,
            m => m is { Name: "voltage", Value: float fv } && Math.Abs(fv - 230.5f) < 0.001f);
        Assert.Contains(template.Metrics,
            m => m is { Name: "current", Value: float fc } && Math.Abs(fc - 4.2f) < 0.001f);
    }

    [Fact]
    public void ToProtoTemplate_TemplateWithParameters_ConvertsParametersCorrectly()
    {
        var template = new Template
        {
            Version = "1.5",
            Parameters =
            [
                new Parameter { Name = "threshold", Type = DataType.Int32, Value = 100 },
                new Parameter { Name = "mode", Type = DataType.String, Value = "automatic" }
            ]
        };

        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Equal(2, protoTemplate.Parameters.Count);
        Assert.Contains(protoTemplate.Parameters, p => p.Name == "threshold" && p.IntValue == 100);
        Assert.Contains(protoTemplate.Parameters, p => p.Name == "mode" && p.StringValue == "automatic");
    }

    [Fact]
    public void ToTemplate_ProtoTemplateWithParameters_ConvertsParametersCorrectly()
    {
        var protoTemplate = new ProtoTemplate();
        protoTemplate.Parameters.Add(new ProtoTemplate.Types.Parameter
            { Name = "timeout", Type = (uint)DataType.Int32, IntValue = 30 });
        protoTemplate.Parameters.Add(new ProtoTemplate.Types.Parameter
            { Name = "unit", Type = (uint)DataType.String, StringValue = "seconds" });

        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.NotNull(template.Parameters);
        Assert.Equal(2, template.Parameters.Count);
        Assert.Contains(template.Parameters, p => p is { Name: "timeout", Value: 30 });
        Assert.Contains(template.Parameters, p => p.Name == "unit" && p.Value as string == "seconds");
    }

    [Fact]
    public void ToProtoTemplate_EmptyTemplate_ReturnsEmptyProtoTemplate()
    {
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
        var template = new Template { Metrics = [] };
        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Empty(protoTemplate.Metrics);
    }

    [Fact]
    public void ToProtoTemplate_NullParameters_ReturnsEmptyParameters()
    {
        var template = new Template { Parameters = null };
        var protoTemplate = template.ToProtoTemplate();

        Assert.NotNull(protoTemplate);
        Assert.Empty(protoTemplate.Parameters);
    }

    [Fact]
    public void ToTemplate_ZeroLengthVersion_ReturnsNullVersion()
    {
        var protoTemplate = new ProtoTemplate { Version = string.Empty };
        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.Null(template.Version);
    }

    [Fact]
    public void ToTemplate_ZeroLengthTemplateRef_ReturnsNullTemplateRef()
    {
        var protoTemplate = new ProtoTemplate { TemplateRef = string.Empty };
        var template = protoTemplate.ToTemplate();

        Assert.NotNull(template);
        Assert.Null(template.TemplateRef);
    }
}