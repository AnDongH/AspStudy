using System;

namespace Protocol.DataAttribute;

[AttributeUsage(AttributeTargets.Property)]
public class QueryAttribute(string? queryName = null) : Attribute
{
    public string? QueryName { get; set; } = queryName;
}

[AttributeUsage(AttributeTargets.Property)]
public class HeaderAttribute(string? headerName = null) : Attribute
{
    public string? HeaderName { get; set; } = headerName;
}

[AttributeUsage(AttributeTargets.Property)]
public class BodyAttribute(string? bodyName = null) : Attribute
{
    public string? BodyName { get; set; } = bodyName;
}
