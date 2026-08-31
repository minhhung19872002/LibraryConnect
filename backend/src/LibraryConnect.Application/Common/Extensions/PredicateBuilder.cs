using System.Linq.Expressions;

namespace LibraryConnect.Application.Common.Extensions;

/// <summary>
/// Ghép các điều kiện lọc thành một biểu thức duy nhất.
///
/// Tìm kiếm nâng cao của trang tra cứu cho phép nối nhiều mệnh đề bằng VÀ / HOẶC / KHÔNG. Nối bằng
/// VÀ thì chỉ cần gọi Where nhiều lần, nhưng HOẶC thì không: cả nhóm phải nằm trong một biểu thức
/// để dịch xuống một câu lệnh SQL. Lớp này ghép hai biểu thức lại bằng cách thay tham số của vế
/// sau bằng tham số của vế trước — nếu không thay, cây biểu thức có hai tham số khác nhau và trình
/// dịch câu lệnh báo lỗi.
/// </summary>
public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> True<T>() => _ => true;

    public static Expression<Func<T, bool>> False<T>() => _ => false;

    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var parameter = left.Parameters[0];
        var body = Expression.AndAlso(left.Body, Rebind(right, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var parameter = left.Parameters[0];
        var body = Expression.OrElse(left.Body, Rebind(right, parameter));

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return Expression.Lambda<Func<T, bool>>(
            Expression.Not(expression.Body), expression.Parameters[0]);
    }

    private static Expression Rebind<T>(Expression<Func<T, bool>> expression, ParameterExpression parameter) =>
        new ParameterReplacer(expression.Parameters[0], parameter).Visit(expression.Body);

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterReplacer(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _from ? _to : base.VisitParameter(node);
    }
}
