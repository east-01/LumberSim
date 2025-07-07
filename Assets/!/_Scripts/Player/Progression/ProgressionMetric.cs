using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// The ProgressionMetric is a base class for mutliple types of metrics:
///   1. Boolean: A player has a true/false value as the metric
///   2. Integer: The player has done x amount of things
///   3. Float: Same as integer with more precision
/// </summary>
[JsonConverter(typeof(ProgressionMetricConverter))]
[Serializable]
public class ProgressionMetric 
{
    [SerializeField]
    private MetricType type;
    [SerializeField]
    private string name;
    [SerializeField]
    private bool boolValue;
    [SerializeField]
    private int intValue;
    [SerializeField]
    private float floatValue;

    public ProgressionMetric() {}
    public ProgressionMetric(string name, MetricType type, object initValue) 
    {
        this.name = name;
        this.type = type;
        SetValue(initValue);
    }

    /// <summary>
    /// Evaluates the metric against another metric. Has different results based off type:
    ///     - bool: boolValue == other.boolValue
    ///     - int: intValue >= other.intValue
    ///     - float: floatValue == other.floatValue
    /// </summary>
    /// <param name="other">The other progression metric. Should match the type of the current metric.</param>
    /// <returns></returns>
    public bool Evaluate(ProgressionMetric other) {
        if(other.type != type)
            throw new InvalidOperationException($"Other type ({other.type}) mismatches metric type ({type})");

        switch(type) {
            case MetricType.Boolean:
                return GetBoolValue() == other.GetBoolValue();                
            case MetricType.Integer:
                return GetIntValue() >= other.GetIntValue();
            case MetricType.Float:
                return GetFloatValue() >= other.GetFloatValue();
        }

        throw new InvalidOperationException($"Couldn't handle type comparison \"{type}\"");
    }

    public MetricType GetMetricType() => type;
    public string GetName() => name;

    public bool GetBoolValue() 
    {
        if(type != MetricType.Boolean)
            throw new InvalidOperationException($"Tried to get bool metric type when type is actually \"{type}\"");

        return boolValue;
    }

    public int GetIntValue() 
    {
        if(type != MetricType.Integer)
            throw new InvalidOperationException($"Tried to get int metric type when type is actually \"{type}\"");

        return intValue;
    }

    public float GetFloatValue() 
    {
        if(type != MetricType.Float)
            throw new InvalidOperationException($"Tried to get float metric type when type is actually \"{type}\"");

        return floatValue;
    }

    public void SetMetricType(MetricType type) => this.type = type; 
    public void SetName(string name) => this.name = name;

    public void SetValue(object value) 
    {
        switch(type) {
            case MetricType.Boolean:
                if(value.GetType() != typeof(bool))
                    throw new InvalidOperationException("Tried to set metric type as something other than bool");
                this.boolValue = (bool)value;
                break;
            case MetricType.Integer:
                if(value.GetType() != typeof(int))
                    throw new InvalidOperationException("Tried to set metric type as something other than int");
                this.intValue = (int)value;
                break;
            case MetricType.Float:
                if(value.GetType() != typeof(float))
                    throw new InvalidOperationException("Tried to set metric type as something other than float");
                this.floatValue = (float)value;
                break;
        }
    }

    public enum MetricType
    {
        Boolean,
        Integer,
        Float
    }

    public static object GetDefaultValue(MetricType metricType) {
        switch(metricType) {
            case MetricType.Boolean:
                return false;
            case MetricType.Integer:
                return 0;
            case MetricType.Float:
                return 0f;
        }

        throw new InvalidOperationException($"Couldn't get default value for type \"{metricType}\"");
    }
}

public class ProgressionMetricConverter : JsonConverter<ProgressionMetric>
{
    public override void WriteJson(JsonWriter writer, ProgressionMetric pm, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        // name first
        writer.WritePropertyName("name");
        writer.WriteValue(pm.GetName());

        // then type
        writer.WritePropertyName("type");
        writer.WriteValue(pm.GetMetricType().ToString());

        // then value
        writer.WritePropertyName("value");
        switch(pm.GetMetricType())
        {
            case ProgressionMetric.MetricType.Boolean:
                serializer.Serialize(writer, pm.GetBoolValue());
                break;
            case ProgressionMetric.MetricType.Integer:
                serializer.Serialize(writer, pm.GetIntValue());
                break;
            case ProgressionMetric.MetricType.Float:
                serializer.Serialize(writer, pm.GetFloatValue());
                break;
        }

        writer.WriteEndObject();
    }

    public override ProgressionMetric ReadJson(JsonReader reader, Type objectType, ProgressionMetric existing, bool hasExisting, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);

        // read name
        var name = jo["name"]?.ToObject<string>();
        // read type
        var type = jo["type"].ToObject<ProgressionMetric.MetricType>();
        // read value token
        var val  = jo["value"];

        var pm = new ProgressionMetric();
        pm.SetName(name);
        pm.SetMetricType(type);

        // pick the right CLR type for value
        object v = type switch
        {
            ProgressionMetric.MetricType.Boolean => val.ToObject<bool>(),
            ProgressionMetric.MetricType.Integer => val.ToObject<int>(),
            ProgressionMetric.MetricType.Float   => val.ToObject<float>(),
            _ => throw new InvalidOperationException($"Unknown MetricType: {type}")
        };
        pm.SetValue(v);

        return pm;
    }
}