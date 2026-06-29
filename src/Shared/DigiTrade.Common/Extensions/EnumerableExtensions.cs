using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DigiTrade.Common.Extensions;

/// <summary>
/// Enumerable select extensions.
/// </summary>
public static class EnumerableExtensions
{
    public static IEnumerable<SelectResult<TSource, TResult>> TrySelect<TSource, TResult>(
        this IEnumerable<TSource> enumerable,
        Func<TSource, TResult> selector)
    {
        foreach (var element in enumerable)
        {
            SelectResult<TSource, TResult> returnedValue;
            try
            {
                returnedValue = new SelectResult<TSource, TResult>(element, selector(element), null);
            }
            catch (Exception ex)
            {
                returnedValue = new SelectResult<TSource, TResult>(element, default, ex);
            }

            yield return returnedValue;
        }
    }

    public static IEnumerable<TResult> Catch<TSource, TResult>(
        this IEnumerable<SelectResult<TSource, TResult>> enumerable,
        Func<Exception, TResult> exceptionHandler)
    {
        return enumerable.Select(x => x.CaughtException == null ? x.Result! : exceptionHandler(x.CaughtException));
    }

    public static IEnumerable<TResult> Catch<TSource, TResult>(
        this IEnumerable<SelectResult<TSource, TResult>> enumerable,
        Func<TSource, Exception, TResult> exceptionHandler)
    {
        return enumerable.Select(x =>
            x.CaughtException == null ? x.Result! : exceptionHandler(x.Source, x.CaughtException));
    }

    public class SelectResult<TSource, TResult>
    {
        internal SelectResult(TSource source, TResult? result, Exception? exception)
        {
            Source = source;
            Result = result;
            CaughtException = exception;
        }

        public TSource Source { get; private set; }
        public TResult? Result { get; private set; }
        public Exception? CaughtException { get; private set; }
    }

    public static bool IsEmpty(this IEnumerable list)
    {
        return list is null or ICollection {Count: 0} or Array {Length: 0};
    }

    public static bool IsNotEmpty(this IEnumerable list)
    {
        return list is ICollection {Count: > 0} or Array {Length: > 0};
    }

    public static string HasValue([MaybeNull] this IEnumerable list, string result)
    {
        return list != null && list.GetCount() > 0 ? result : string.Empty;
    }
    
    public static int GetCount(this IEnumerable list)
    {
        return list is ICollection collection ? collection.Count : 
            list is Array array ? array.Length
            : 0;
    }

    public static bool Is(this IEnumerable? list1, IEnumerable? list2)
    {
        if(list1 == null || list2 == null) return false;
        var list1AsString = JsonSerializer.Serialize(list1);
        var list2AsString = JsonSerializer.Serialize(list2);
        return list1AsString.Is(list2AsString);
    }

    public static bool IsNot(this IEnumerable list1, IEnumerable list2)
    {
        return !list1.Is(list2);
    }
}