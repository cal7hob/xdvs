using System;
using System.Collections.Generic;

public class CustomEventHitBuilder
{
    private static readonly Type gaEventCategoryKeysType    = typeof(GAEvent.Category);
    private static readonly Type gaEventActionKeysType      = typeof(GAEvent.Action);
    private static readonly Type gaEventLabelKeysType       = typeof(GAEvent.Label);

    private readonly Dictionary<Type, string> gaEventParameters;

    private long value;
    private Type lastGAEventParameter;

    public CustomEventHitBuilder()
    {
        gaEventParameters = new Dictionary<Type, string>
        {
            { gaEventCategoryKeysType,  string.Empty },
            { gaEventActionKeysType,    string.Empty },
            { gaEventLabelKeysType,     string.Empty }
        };
    }

    public override string ToString()
    {
        string result = "CustomEventHitBuilder { ";

        foreach (var gaEventParameter in gaEventParameters)
            result += string.Format("{0}: {1}, ", gaEventParameter.Key, gaEventParameter.Value);

        result = result.TrimEnd();
        result = result.TrimEnd(',');

        result += " }";

        return result;
    }

    public CustomEventHitBuilder SetParameter<TGAEventParameter>()
    {
        Type gaEventParameterType = typeof(TGAEventParameter);

        if (!gaEventParameters.ContainsKey(gaEventParameterType))
        {
            DT.LogError("Invalid type argument passed to CustomEventHitBuilder. It's had to be enum key from GAEvent namespace!");
            return null;
        }

        lastGAEventParameter = gaEventParameterType;
        return this;
    }

    public CustomEventHitBuilder SetParameter(GAEvent.Category category)
    {
        return SetGAEventParameter(category);
    }

    public CustomEventHitBuilder SetParameter(GAEvent.Action action)
    {
        return SetGAEventParameter(action);
    }

    public CustomEventHitBuilder SetParameter(GAEvent.Label label)
    {
        return SetGAEventParameter(label);
    }

    public CustomEventHitBuilder SetSubject(GAEvent.Subject subject)
    {
        SetSubject(subject, null);
        return this;
    }

    public CustomEventHitBuilder SetSubject(GAEvent.Subject subject, object id)
    {
        if (lastGAEventParameter == null)
            return this;

        gaEventParameters[lastGAEventParameter]
            += string.Format(
                "{0}{1}{2}{3}",
                gaEventParameters[lastGAEventParameter].Length > 0
                    ? " "
                    : string.Empty,
                subject.ToFriendlyString(),
                id == null ? string.Empty : " ",
                id ?? string.Empty);

        return this;
    }

    public CustomEventHitBuilder SetValue(long number)
    {
        value = number;
        return this;
    }


    public EventHitBuilder ToEventHitBuilder()
    {
        return new EventHitBuilder()
            .SetEventCategory(gaEventParameters[gaEventCategoryKeysType])
            .SetEventAction(gaEventParameters[gaEventActionKeysType])
            .SetEventLabel(gaEventParameters[gaEventLabelKeysType])
            .SetEventValue(value);
    }

    private CustomEventHitBuilder SetGAEventParameter(Enum gaEventParameter)
    {
        Type tGAEventParameterType = gaEventParameter.GetType();

        gaEventParameters[tGAEventParameterType] = gaEventParameter.ToFriendlyString();

        lastGAEventParameter = tGAEventParameterType;

        return this;
    }
}
