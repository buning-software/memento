using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Memento.Core.Storage;
using Memento.EntityFramework.Internal;

namespace Memento.EntityFramework.UnitTests.Internal;

public sealed class StorageErrorsTests
{
    public static TheoryData<Exception> StorageFailures() =>
    [
        new SqliteException("database is locked", 5),
        new DbUpdateException("save failed", new SqliteException("database is locked", 5)),
        new FormatException("bad date"),
        new InvalidOperationException("bad state"),
    ];

    [Theory]
    [MemberData(nameof(StorageFailures))]
    public void Storage_failures_leave_as_a_storage_exception_naming_the_cause(Exception failure)
    {
        var error = Assert.Throws<StorageException>(() => StorageErrors.Wrap(() => throw failure));

        Assert.Equal($"The task database could not be used: {failure.GetBaseException().Message}", error.Message);
        Assert.Same(failure, error.InnerException);
    }

    [Fact]
    public void Other_exceptions_pass_through_unchanged()
    {
        Assert.Throws<ArgumentException>(() => StorageErrors.Wrap(() => throw new ArgumentException("bug")));
    }

    [Fact]
    public void The_result_of_the_work_is_returned()
    {
        Assert.Equal(42, StorageErrors.Wrap(() => 42));
    }

    [Fact]
    public void Work_without_a_result_runs_exactly_once()
    {
        int runs = 0;

        StorageErrors.Wrap(() => runs++);

        Assert.Equal(1, runs);
    }

    [Theory]
    [MemberData(nameof(StorageFailures))]
    public void Work_without_a_result_wraps_storage_failures_too(Exception failure)
    {
        var error = Assert.Throws<StorageException>(() => StorageErrors.Wrap(new Action(() => throw failure)));

        Assert.Same(failure, error.InnerException);
    }

    [Fact]
    public void Other_exceptions_from_work_without_a_result_pass_through_unchanged()
    {
        Assert.Throws<ArgumentException>(() => StorageErrors.Wrap(new Action(() => throw new ArgumentException("bug"))));
    }

    [Fact]
    public void The_message_names_the_innermost_cause()
    {
        var failure = new InvalidOperationException("outer", new FormatException("innermost cause"));

        var error = Assert.Throws<StorageException>(() => StorageErrors.Wrap(() => throw failure));

        Assert.Contains("innermost cause", error.Message);
        Assert.DoesNotContain("outer", error.Message);
    }
}
