using FluentAssertions;

namespace Khitiara.Utils.Tests.Unit;

public static class CollectTests
{
    public static TheoryData<int?[], int[]> CollectNullTestData() => new() {
        { Array.Empty<int?>(), Array.Empty<int>() },
        { new int?[] { 1, null, 2, null, 3, }, new[] { 1, 2, 3, } },
        { new int?[] { null, null, }, Array.Empty<int>() },
    };

    public static TheoryData<string?[], int[]> CollectWithDelegateTestData() => new() {
        { Array.Empty<string?>(), Array.Empty<int>() },
        { new string?[] { null, null, }, Array.Empty<int>() },
        { new[] { "5", null, "3", }, new[] { 5, 3, } },
    };

    [Theory, MemberData(nameof(CollectNullTestData))]
    public static void TestCollectNull(int?[] test, int[] expected) {
        test.Collect().Should().BeEquivalentTo(expected);
    }

    [Theory, MemberData(nameof(CollectWithDelegateTestData)),]
    public static void TestCollectDelegate(string?[] test, int[] expected) {
        test.Collect((string? s, out int @out) => int.TryParse(s, out @out)).Should().BeEquivalentTo(expected);
    }
}