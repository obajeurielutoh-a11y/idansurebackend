// File: SubscriptionSystem.Application/Common/ExpressionExtensions.cs
using System;
using System.Linq.Expressions;

namespace SubscriptionSystem.Application.Common
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> And<T>(
            this Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            var parameter = Expression.Parameter(typeof(T));

            var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], parameter);
            var leftExpression = leftVisitor.Visit(left.Body);

            var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);
            var rightExpression = rightVisitor.Visit(right.Body);

            var combinedExpression = Expression.AndAlso(leftExpression, rightExpression);

            return Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        }

        private class ReplaceExpressionVisitor : ExpressionVisitor
        {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
            {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit(Expression node)
            {
                if (node == _oldValue)
                    return _newValue;

                return base.Visit(node);
            }
        }
    }
}