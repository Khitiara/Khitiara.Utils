using System.Diagnostics.CodeAnalysis;

namespace Khitiara.Utils;

/// <summary>
/// A Try-type method: a method which returns bool and has a single nullable out parameter which is guaranteed
/// a non-null value when the method returns true.
/// </summary>
public delegate bool TryDelegate<in TIn, TOut>(TIn source, [NotNullWhen(true)] out TOut? output);