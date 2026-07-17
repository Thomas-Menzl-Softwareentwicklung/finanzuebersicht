using System.Linq.Expressions;
using NSubstitute;
using NSubstitute.Core;

namespace Finanzuebersicht.Tests.TestHelpers;

public static class NonNullArg
{
    public static T Is<T>(Expression<Predicate<T>> predicate)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate);
        var compiledPredicate = predicate.Compile();

        return Arg.Is<T>(value => value != null && compiledPredicate(value))!;
    }

    public static T Do<T>(Action<T> action)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(action);

        return Arg.Do<T>(value => action(RequireNotNull(value)))!;
    }

    public static T ArgNotNull<T>(this CallInfo callInfo)
        where T : class
        => RequireNotNull(callInfo.Arg<T>());

    public static T ArgAtNotNull<T>(this CallInfo callInfo, int position)
        where T : class
        => RequireNotNull(callInfo.ArgAt<T>(position));

    private static T RequireNotNull<T>(T? value)
        where T : class
        => value ?? throw new InvalidOperationException($"Expected a non-null {typeof(T).Name} argument.");
}
