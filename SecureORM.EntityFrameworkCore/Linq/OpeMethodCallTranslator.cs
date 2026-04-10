using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using SecureORM.Core.Encoding;

namespace SecureORM.EntityFrameworkCore.Linq;

/// <summary>
/// Translates OpeQueryExtensions marker methods into SQL expressions.
/// </summary>
public class OpeMethodCallTranslator : IMethodCallTranslator
{
    private readonly OPEEncoder _encoder;
    private readonly ISqlExpressionFactory _sqlExpressionFactory;

    private static readonly MethodInfo StringStartsWithMethod =
        typeof(OpeQueryExtensions).GetMethod(nameof(OpeQueryExtensions.OpeStartsWith),
            new[] { typeof(string), typeof(string) })!;

    private static readonly MethodInfo StringInRangeMethod =
        typeof(OpeQueryExtensions).GetMethod(nameof(OpeQueryExtensions.OpeInRange),
            new[] { typeof(string), typeof(string), typeof(string) })!;

    private static readonly MethodInfo LongInRangeMethod =
        typeof(OpeQueryExtensions).GetMethod(nameof(OpeQueryExtensions.OpeInRange),
            new[] { typeof(long), typeof(long), typeof(long) })!;

    private static readonly MethodInfo DecimalInRangeMethod =
        typeof(OpeQueryExtensions).GetMethod(nameof(OpeQueryExtensions.OpeInRange),
            new[] { typeof(decimal), typeof(decimal), typeof(decimal) })!;

    private static readonly MethodInfo IntInRangeMethod =
        typeof(OpeQueryExtensions).GetMethod(nameof(OpeQueryExtensions.OpeInRange),
            new[] { typeof(int), typeof(int), typeof(int) })!;

    public OpeMethodCallTranslator(OPEEncoder encoder, ISqlExpressionFactory sqlExpressionFactory)
    {
        _encoder = encoder;
        _sqlExpressionFactory = sqlExpressionFactory;
    }

    public SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method == StringStartsWithMethod)
        {
            return TranslateStartsWith(arguments);
        }

        if (method == StringInRangeMethod || method == LongInRangeMethod ||
            method == DecimalInRangeMethod || method == IntInRangeMethod)
        {
            return TranslateInRange(arguments);
        }

        return null;
    }

    private SqlExpression? TranslateStartsWith(IReadOnlyList<SqlExpression> arguments)
    {
        // arguments[0] = column, arguments[1] = prefix constant
        var column = arguments[0];
        if (arguments[1] is not SqlConstantExpression prefixConstant || prefixConstant.Value is not string prefix)
            return null;

        var encodedPrefix = _encoder.EncodePrefix(prefix);
        var pattern = _sqlExpressionFactory.Constant(encodedPrefix + "%");

        return _sqlExpressionFactory.Like(column, pattern);
    }

    private SqlExpression? TranslateInRange(IReadOnlyList<SqlExpression> arguments)
    {
        // arguments[0] = column, arguments[1] = low, arguments[2] = high
        var column = arguments[0];

        // Get the encoded low and high from constants
        string? encodedLow = EncodeConstant(arguments[1]);
        string? encodedHigh = EncodeConstant(arguments[2]);

        if (encodedLow == null || encodedHigh == null)
            return null;

        var lowExpr = _sqlExpressionFactory.Constant(encodedLow);
        var highExpr = _sqlExpressionFactory.Constant(encodedHigh);

        var gte = _sqlExpressionFactory.GreaterThanOrEqual(column, lowExpr);
        var lte = _sqlExpressionFactory.LessThanOrEqual(column, highExpr);

        return _sqlExpressionFactory.AndAlso(gte, lte);
    }

    private string? EncodeConstant(SqlExpression expression)
    {
        if (expression is not SqlConstantExpression constant || constant.Value == null)
            return null;

        return constant.Value switch
        {
            string s => _encoder.EncodeString(s),
            long l => _encoder.EncodeInteger(l),
            int i => _encoder.EncodeInteger(i),
            decimal d => _encoder.EncodeDecimal(d),
            _ => null
        };
    }
}
