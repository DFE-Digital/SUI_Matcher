using Shared.Models;

using SUI.Client.Core.Application.UseCases.ReconcilePeople;

namespace Unit.Tests.Client;

public class ReconciliationStatsTests
{
    [Fact]
    public void MissingLocal_ShouldIncrementDifferenceOnGivenField()
    {
        // Arrange
        string[] differencesFields = ["BirthDate"];
        string[] missingLocalFields = [];
        string[] missingNhsFields = [];

        // Act
        var stats = new ReconciliationCsvProcessStats();
        stats.RecordReconciliationStatusStats(ReconciliationStatus.Differences,
            differencesFields, missingLocalFields, missingNhsFields);

        // Assert
        Assert.Equal(1, stats.BirthDateDifferentCount);
        Assert.Equal(0, stats.BirthDateLaMissingCount);
        Assert.Equal(0, stats.BirthDateNhsMissingCount);
        Assert.Equal(0, stats.BirthDateBothMissingCount);
    }

    [Fact]
    public void MissingLocal_ShouldIncrementMissingLocalOnGivenFields()
    {
        // Arrange
        string[] differencesFields = [];
        string[] missingLocalFields = ["BirthDate"];
        string[] missingNhsFields = [];

        // Act
        var stats = new ReconciliationCsvProcessStats();
        stats.RecordReconciliationStatusStats(ReconciliationStatus.Differences,
            differencesFields, missingLocalFields, missingNhsFields);

        // Assert
        Assert.Equal(1, stats.BirthDateLaMissingCount);
        Assert.Equal(0, stats.BirthDateNhsMissingCount);
        Assert.Equal(0, stats.BirthDateBothMissingCount);
    }

    [Fact]
    public void MissingLocal_ShouldIncrementMissingNhsOnGivenFields()
    {
        // Arrange
        string[] differencesFields = [];
        string[] missingLocalFields = [];
        string[] missingNhsFields = ["Given"];

        // Act
        var stats = new ReconciliationCsvProcessStats();
        stats.RecordReconciliationStatusStats(ReconciliationStatus.Differences,
            differencesFields, missingLocalFields, missingNhsFields);

        // Assert
        Assert.Equal(1, stats.GivenNameNhsMissingCount);
        Assert.Equal(0, stats.GivenNameLaMissingCount);
        Assert.Equal(0, stats.GivenNameBothMissingCount);
    }

    [Fact]
    public void MissingLocal_ShouldIncrementBothMissing_WhenFieldIsMissingOnBoth()
    {
        // Arrange
        string[] differencesFields = [];
        string[] missingLocalFields = ["Given"];
        string[] missingNhsFields = ["Given"];

        // Act
        var stats = new ReconciliationCsvProcessStats();
        stats.RecordReconciliationStatusStats(ReconciliationStatus.Differences,
            differencesFields, missingLocalFields, missingNhsFields);

        // Assert
        Assert.Equal(0, stats.GivenNameNhsMissingCount);
        Assert.Equal(0, stats.GivenNameLaMissingCount);
        Assert.Equal(1, stats.GivenNameBothMissingCount);
    }

    [Fact]
    public void RecordReconciliationStatusStats_ShouldOnlyIncrementDifferencesCount_WhenFieldsAreDifferent()
    {
        // Arrange
        string[] differencesFields = ["BirthDate", "Given"];
        string[] missingLocalFields = ["Email"];
        string[] missingNhsFields = ["Phone"];

        // Act
        var stats = new ReconciliationCsvProcessStats();
        stats.RecordReconciliationStatusStats(ReconciliationStatus.Differences,
            differencesFields, missingLocalFields, missingNhsFields);

        // Assert
        // DifferencesCount is for when a record contains one or more differences.
        Assert.Equal(1, stats.DifferencesCount);

        // Verify the actual differences were recorded
        Assert.Equal(1, stats.BirthDateDifferentCount);
        Assert.Equal(1, stats.GivenNameDifferenceCount);

        // Verify missing fields were recorded but didn't increment DifferencesCount
        Assert.Equal(1, stats.EmailLaMissingCount);
        Assert.Equal(1, stats.PhoneNhsMissingCount);
    }

}