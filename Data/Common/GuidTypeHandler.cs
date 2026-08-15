using System.Data;
using Dapper;

namespace SnowflakeDapperExample.Data.Common;

public sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value)
    {
        return value switch
        {
            Guid guid => guid,
            string text => Guid.Parse(text),
            _ => Guid.Parse(value.ToString() ?? string.Empty)
        };
    }

    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToString("D");
    }
}
