using Dapper;
using System.Data;

namespace EventManager.Infrastructure.TypeHandlers;

/// <summary>
/// Dapper type handler that maps between <see cref="Guid"/> and its string representation.
/// Required for SQLite, which stores GUIDs as TEXT with no native GUID type.
/// </summary>
public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
        => parameter.Value = value.ToString();

    public override Guid Parse(object value)
        => Guid.Parse((string)value);
}
