using System;
using System.Collections.Generic;

public class ChangeElementStateRequestInfo
{
    public object sender;
    public HashSet<Type> recipients;
    public bool state;

    public ChangeElementStateRequestInfo(object _sender, HashSet<Type> _recipients, bool _state)
    {
        sender = _sender;
        recipients = _recipients;
        state = _state;
    }

    public ChangeElementStateRequestInfo(object _sender, Type _recipient, bool _state)
    {
        sender = _sender;
        recipients = new HashSet<Type>() { _recipient };
        state = _state;
    }

    public bool ForMe(Type t)
    {
        if (recipients == null)
            return false;
        return recipients.Contains(t);
    }
}

