using System.Collections.Concurrent;

namespace Khitiara.Utils;

public static class EnumerableExtensions
{
    /// <summary>
    /// Collects non-null items from an input enumerable.
    /// This method is strictly equivalent to <code>source.Where(i => i != null)</code> except it satisfies nullability
    /// checks.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{T}"/> whose non-null elements should be iterated</param>
    /// <typeparam name="T">The type of input elements</typeparam>
    /// <returns>The sequence of non-null input elements</returns>
    public static IEnumerable<T> Collect<T>(this IEnumerable<T?> source) {
        foreach (T? item in source) {
            if (item is { } i)
                yield return i;
        }
    }

    /// <summary>
    /// Collect items from an enumerable using a try-function e.g. <code>bool TryParse(string, out T?)</code>.
    /// The provided try-function is assumed to satisfy the condition that the out parameter will be non-null if
    /// the function returns true.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{TIn}"/> whose elements should be collected</param>
    /// <param name="tryDelegate">A <see cref="TryDelegate{TIn,TOut}"/> which provides and determines which elements to collect</param>
    /// <typeparam name="TIn">The input element type</typeparam>
    /// <typeparam name="TOut">The output element type</typeparam>
    /// <returns>The sequence of processed and collected elements</returns>
    public static IEnumerable<TOut> Collect<TIn, TOut>(this IEnumerable<TIn> source, TryDelegate<TIn, TOut> tryDelegate) {
        foreach (TIn @in in source) {
            if (tryDelegate(@in, out TOut? t))
                yield return t;
        }
    }

    /// <summary>
    /// Executes the provided action in parallel on all elements of the provided <see cref="IEnumerable{T}"/>.
    ///
    /// When <paramref name="source"/> is a <see cref="ParallelQuery{T}"/> then <see cref="ParallelEnumerable.ForAll{T}"/>
    /// is used to respect the partitioning options of the <see cref="ParallelQuery{T}"/>.
    ///
    /// No ordering or thread-safety guarantee is provided.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{T}"/> whose elements should be passed to <paramref name="action"/></param>
    /// <param name="action">The action to execute on each element of the source enumerable.</param>
    /// <typeparam name="T">The type of elements of <paramref name="source"/></typeparam>
    public static void ParallelForEach<T>(this IEnumerable<T> source, Action<T> action) {
        if (source is ParallelQuery<T> query)
            query.ForAll(action);
        else
            Parallel.ForEach(source, action);
    }

    /// <summary>
    /// Creates a <see cref="ConcurrentBag{T}"/> from an <see cref="IEnumerable{T}"/>, adding items in parallel from the original source.
    /// 
    /// When <paramref name="source"/> is a <see cref="ParallelQuery{T}"/> then <see cref="ParallelEnumerable.ForAll{T}"/>
    /// is used to respect the partitioning options of the <see cref="ParallelQuery{T}"/>.
    ///
    /// No ordering guarantee is provided.
    /// </summary>
    /// <param name="source">The <see cref="IEnumerable{T}"/> to create a <see cref="ConcurrentBag{T}"/> from.</param>
    /// <typeparam name="T">The type of elements of <paramref name="source"/></typeparam>
    /// <returns>A <see cref="ConcurrentBag{T}"/> that contains elements from the input sequence</returns>
    public static ConcurrentBag<T> ParallelToBag<T>(this IEnumerable<T> source) {
        ConcurrentBag<T> bag = new();
        source.ParallelForEach(bag.Add);
        return bag;
    }

    /// <summary>
    /// Execute an asynchronous operation on each element of a <see cref="ParallelQuery{T}"/> and returns a task
    /// which will be complete when all the asynchronous operations complete.
    /// </summary>
    /// <param name="source">The <see cref="ParallelQuery{T}"/> whose elements should be passed to <paramref name="func"/></param>
    /// <param name="func">The asynchronous action to execute on each element of the source enumerable.</param>
    /// <typeparam name="T">The type of elements of <paramref name="source"/></typeparam>
    /// <returns>A <see cref="Task"/> which will complete when all elements of the source sequence have been processed</returns>
    public static Task ForAllAsync<T>(this ParallelQuery<T> source, Func<T, Task> func) =>
        Task.WhenAll(source.Select(func).ParallelToBag());

    /// <summary>
    /// Execute an asynchronous operation on each element of a <see cref="ParallelQuery{T}"/> and returns a task
    /// which will be complete when all the asynchronous operations complete.
    /// </summary>
    /// <param name="source">The <see cref="ParallelQuery{T}"/> whose elements should be passed to <paramref name="func"/></param>
    /// <param name="func">The asynchronous action to execute on each element of the source enumerable.</param>
    /// <param name="state">Global state, which will be passed to <paramref name="func"/></param>
    /// <typeparam name="T">The type of elements of <paramref name="source"/></typeparam>
    /// <typeparam name="TState">A global state type which will be forwarded to <paramref name="func"/></typeparam>
    /// <returns>A <see cref="Task"/> which will complete when all elements of the source sequence have been processed</returns>
    public static Task ForAllAsync<T, TState>(this ParallelQuery<T> source, Func<T, TState, Task> func,
        TState state) =>
        Task.WhenAll(source.Select(i => func(i, state)).ParallelToBag());

    /// <summary>
    /// Execute an asynchronous operation on each element of a <see cref="ParallelQuery{T}"/> and returns a task
    /// which will be complete when all the asynchronous operations complete.
    /// 
    /// This overload calls <see cref="ParallelEnumerable.WithCancellation{T}"/> and as such cannot be used
    /// with queries pre-configured with cancellation support.
    /// </summary>
    /// <param name="source">The <see cref="ParallelQuery{T}"/> whose elements should be passed to <paramref name="func"/></param>
    /// <param name="func">The asynchronous action to execute on each element of the source enumerable.</param>
    /// <param name="cancellationToken">A cancellation token to pass to <paramref name="func"/> which will cancel operation</param>
    /// <typeparam name="T">The type of elements of <paramref name="source"/></typeparam>
    /// <returns>A <see cref="Task"/> which will complete when all elements of the source sequence have been processed</returns>
    public static Task ForAllAsync<T>(this ParallelQuery<T> source, Func<T, CancellationToken, Task> func,
        CancellationToken cancellationToken = default) {
        ConcurrentBag<Task> bag = new();
        source.WithCancellation(cancellationToken).ForAll(item => {
            cancellationToken.ThrowIfCancellationRequested();
            bag.Add(func(item, cancellationToken));
        });
        return Task.WhenAll(bag.ToArray());
    }
}