using System.Linq.Expressions;

public static class StringExtensions
{
    public static string Parse<T>(this string item)
    {
        var typename = typeof(T).ToString();
        item = item
            .Remove("(")
            .Remove(")")
            .Replace("AndAlso", "&&")
            .Replace(".Contains", " ~ ")
            .Replace("==", "=")
            .Replace("||", "OR")
            .Replace("!=", "<>")
            .Replace('"', '\'')
            .Replace("x.", "") //FIX: This needs dot notation
            ;
        return item;
    }

    public static string Remove(this string item, string character)
    {
        item = item.Replace(character, "");
        return item;
    }
}

public class ParameterlessExpressionSearcher : ExpressionVisitor
{
    public HashSet<Expression> ParameterlessExpressions { get; } = new HashSet<Expression>();
    private bool containsParameter = false;

    public override Expression Visit(Expression node)
    {
        bool originalContainsParameter = containsParameter;
        containsParameter = false;
        base.Visit(node);
        if (!containsParameter)
        {
            if (node?.NodeType == ExpressionType.Parameter)
                containsParameter = true;
            else
                ParameterlessExpressions.Add(node);
        }
        containsParameter |= originalContainsParameter;

        return node;
    }
}

public class ParameterlessExpressionEvaluator : ExpressionVisitor
{
    private HashSet<Expression> parameterlessExpressions;
    public ParameterlessExpressionEvaluator(HashSet<Expression> parameterlessExpressions)
    {
        this.parameterlessExpressions = parameterlessExpressions;
    }
    public override Expression Visit(Expression node)
    {
        if (parameterlessExpressions.Contains(node))
            return Evaluate(node);
        else
            return base.Visit(node);
    }

    private Expression Evaluate(Expression node)
    {
        if (node.NodeType == ExpressionType.Constant)
        {
            return node;
        }
        object value = Expression.Lambda(node).Compile().DynamicInvoke();
        return Expression.Constant(value, node.Type);
    }
}
public static class ExpressionExtensions
{
    public static Expression Simplify(this Expression expression)
    {
        var searcher = new ParameterlessExpressionSearcher();
        searcher.Visit(expression);
        return new ParameterlessExpressionEvaluator(searcher.ParameterlessExpressions).Visit(expression);
    }

    public static Expression<Func<T, bool>> Simplify<T>(this Expression<Func<T, bool>> expression)
    {
        return (Expression<Func<T, bool>>)Simplify((Expression)expression);
    }

    //all previously shown code goes here

}
