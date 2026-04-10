using Microsoft.EntityFrameworkCore.Query;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Linq;

public class OpeMethodCallTranslatorPlugin : IMethodCallTranslatorPlugin
{
    public OpeMethodCallTranslatorPlugin(
        OPEEncoder encoder,
        ISqlExpressionFactory sqlExpressionFactory)
    {
        Translators = new IMethodCallTranslator[]
        {
            new OpeMethodCallTranslator(encoder, sqlExpressionFactory)
        };
    }

    public IEnumerable<IMethodCallTranslator> Translators { get; }
}
