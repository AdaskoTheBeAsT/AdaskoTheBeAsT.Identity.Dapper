using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace AdaskoTheBeAsT.Identity.Dapper.SourceGenerator.Builders;

public class SqlExpressionVisitor(char parameterPrefix)
    : ExpressionVisitor
{
    private readonly StringBuilder _sqlBuilder = new();

    private readonly Dictionary<string, object> _parameters = new(StringComparer.Ordinal);

    private int _parameterIndex;

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public string Translate(Expression expression)
    {
        Visit(expression);
        return _sqlBuilder.ToString();
    }

    public IDictionary<string, object> GetParameters() => _parameters;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Queryable) &&
            (string.Equals(node.Method.Name, nameof(Skip), StringComparison.Ordinal) || string.Equals(
                node.Method.Name,
                nameof(Take),
                StringComparison.Ordinal)))
        {
            if (string.Equals(node.Method.Name, nameof(Skip), StringComparison.Ordinal))
            {
                Skip = (int)((ConstantExpression)node.Arguments[1]).Value;
            }
            else if (string.Equals(node.Method.Name, nameof(Take), StringComparison.Ordinal))
            {
                Take = (int)((ConstantExpression)node.Arguments[1]).Value;
            }

            Visit(node.Arguments[0]); // Continue processing the source IQueryable
            return node;
        }

        if (node.Method.DeclaringType == typeof(string) && string.Equals(node.Method.Name, "Contains", StringComparison.Ordinal))
        {
            Visit(node.Object);
            _sqlBuilder.Append(" LIKE ");
            var parameterName = $"{parameterPrefix}p{_parameterIndex++}";
            _sqlBuilder.Append(parameterName);
            _parameters[parameterName] = $"%{GetValue(node.Arguments[0])}%";
            return node;
        }

        if (node.Method.DeclaringType == typeof(string) && string.Equals(node.Method.Name, "Length", StringComparison.Ordinal))
        {
            _sqlBuilder.Append("LEN(");
            Visit(node.Object);
            _sqlBuilder.Append(')');
            return node;
        }

        throw new NotSupportedException($"The method '{node.Method.Name}' is not supported");
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _sqlBuilder.Append("(");
        Visit(node.Left);

        if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
        {
            if (node.Right is ConstantExpression { Value: null })
            {
                _sqlBuilder.Append(node.NodeType == ExpressionType.Equal ? " IS NULL" : " IS NOT NULL");
            }
            else
            {
                _sqlBuilder.Append($" {GetSqlOperator(node.NodeType)} ");
                Visit(node.Right);
            }
        }
        else
        {
            _sqlBuilder.Append($" {GetSqlOperator(node.NodeType)} ");
            Visit(node.Right);
        }

        _sqlBuilder.Append(')');
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is { NodeType: ExpressionType.Parameter })
        {
            _sqlBuilder.Append(node.Member.Name);
            return node;
        }

        var constantValue = GetValue(node);
        if (constantValue != null)
        {
            var parameterName = $"{parameterPrefix}p{_parameterIndex++}";
            _sqlBuilder.Append(parameterName);
            _parameters[parameterName] = constantValue;
        }
        else
        {
            _sqlBuilder.Append("NULL");
        }

        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value == null)
        {
            _sqlBuilder.Append("NULL");
        }
        else
        {
            var parameterName = $"{parameterPrefix}p{_parameterIndex++}";
            _sqlBuilder.Append(parameterName);
            _parameters[parameterName] = node.Value;
        }

        return node;
    }

    private static string GetSqlOperator(ExpressionType nodeType) =>
        nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Unsupported operator: {nodeType}"),
        };

    private static object? GetValue(Expression member)
    {
        switch (member.NodeType)
        {
            case ExpressionType.Constant:
                return ((ConstantExpression)member).Value;
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)member;
                var constantExpression = (ConstantExpression)memberExpression.Expression;
                var fieldInfo = (FieldInfo)memberExpression.Member;
                return fieldInfo.GetValue(constantExpression.Value);
            default:
                throw new NotSupportedException($"The expression type '{member.NodeType}' is not supported");
        }
    }
}
