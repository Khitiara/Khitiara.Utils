using FluentAssertions;
using Moq;

namespace Khitiara.Utils.Tests.Unit;

public static class ForAllTests
{
    public static TheoryData<int[]> ToBagTestData() => new() {
        new[] { 5, 3, },
    };

    [Fact]
    public static void TestToBagEmpty() {
        Array.Empty<int>().ParallelToBag().Should().BeEmpty();
    }

    [Theory, MemberData(nameof(ToBagTestData)),]
    public static void TestToBag(int[] data) {
        data.ParallelToBag().Should().HaveSameCount(data).And.Contain(data);
    }

    [Fact]
    public static void TestForAllSimple() {
        Mock<Action<int>> mock = new();
        int[] srcData = { 3, 5, 2, };
        srcData.AsParallel().ParallelForEach(mock.Object);
        mock.Verify(a => a(2));
        mock.Verify(a => a(3));
        mock.Verify(a => a(5));
        mock.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task TestForAllAsync() {
        Mock<Func<int, Task>> mock = new();
        mock.Setup(a => a(It.IsAny<int>()))
            .Returns<int>(async arg => await Task.Delay(TimeSpan.FromMilliseconds(arg)));
        int[] srcData = { 3, 5, 2, };
        await srcData.AsParallel().ForAllAsync(mock.Object);
        mock.Verify(a => a(2));
        mock.Verify(a => a(3));
        mock.Verify(a => a(5));
        mock.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task TestForAllStateAsync() {
        Mock<Func<int, int, Task>> mock = new();
        mock.Setup(a => a(It.IsAny<int>(), 5))
            .Returns<int, int>(async (arg, _) => await Task.Delay(TimeSpan.FromMilliseconds(arg)));
        int[] srcData = { 3, 5, 2, };
        await srcData.AsParallel().ForAllAsync(mock.Object, 5);
        mock.Verify(a => a(2, 5));
        mock.Verify(a => a(3, 5));
        mock.Verify(a => a(5, 5));
        mock.VerifyNoOtherCalls();
    }

    [Fact]
    public static async Task TestForAllCancellationAsync() {
        Mock<Func<int, CancellationToken, Task>> mock = new();
        mock.Setup(a => a(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns<int, CancellationToken>(async (arg, cancellationToken) =>
                await Task.Delay(TimeSpan.FromMilliseconds(arg), cancellationToken));
        int[] srcData = { 3, 5, 2, };
        CancellationTokenSource cts = new();
        await new Func<Task>(() => srcData.AsParallel().ForAllAsync(mock.Object, cts.Token))
            .Should().NotThrowAsync();
        mock.Verify(a => a(2, cts.Token));
        mock.Verify(a => a(3, cts.Token));
        mock.Verify(a => a(5, cts.Token));
        mock.VerifyNoOtherCalls();
        cts.Cancel();
        await new Func<Task>(() => srcData.AsParallel().ForAllAsync(mock.Object, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}