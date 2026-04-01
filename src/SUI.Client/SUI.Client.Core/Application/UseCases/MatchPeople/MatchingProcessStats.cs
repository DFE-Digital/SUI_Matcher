using Shared.Models;
using SUI.Client.Core.Application.Interfaces;

namespace SUI.Client.Core.Application.UseCases.MatchPeople;

public class MatchingProcessStats : IStats
{
    public int Count { get; set; }
    public int ErroredCount { get; set; }
    public int CountMatched { get; set; }
    public int CountPotentialMatch { get; set; }
    public int CountLowConfidenceMatch { get; set; }
    public int CountManyMatch { get; set; }
    public int CountNoMatch { get; set; }

    private readonly Lazy<double> _erroredPercentage;
    private readonly Lazy<double> _matchedPercentage;
    private readonly Lazy<double> _potentialMatchPercentage;
    private readonly Lazy<double> _lowConfidenceMatchPercentage;
    private readonly Lazy<double> _manyMatchPercentage;
    private readonly Lazy<double> _noMatchPercentage;

    public MatchingProcessStats()
    {
        _erroredPercentage = new Lazy<double>(() => ComputePercentage(ErroredCount));
        _matchedPercentage = new Lazy<double>(() => ComputePercentage(CountMatched));
        _potentialMatchPercentage = new Lazy<double>(() => ComputePercentage(CountPotentialMatch));
        _lowConfidenceMatchPercentage = new Lazy<double>(() =>
            ComputePercentage(CountLowConfidenceMatch)
        );
        _manyMatchPercentage = new Lazy<double>(() => ComputePercentage(CountManyMatch));
        _noMatchPercentage = new Lazy<double>(() => ComputePercentage(CountNoMatch));
    }

    private double ComputePercentage(int count) =>
        Count == 0 ? 0 : Math.Round((double)count / Count * 100, 2);

    public double ErroredPercentage => _erroredPercentage.Value;
    public double MatchedPercentage => _matchedPercentage.Value;
    public double PotentialMatchPercentage => _potentialMatchPercentage.Value;
    public double LowConfidenceMatchPercentage => _lowConfidenceMatchPercentage.Value;
    public double ManyMatchPercentage => _manyMatchPercentage.Value;
    public double NoMatchPercentage => _noMatchPercentage.Value;

    public void RecordStats(PersonMatchResponse? response)
    {
        Count++;

        var matchStatus = response?.Result?.MatchStatus;
        if (matchStatus is null)
        {
            ErroredCount++;
            return;
        }

        switch (matchStatus)
        {
            case MatchStatus.Match:
                CountMatched++;
                break;
            case MatchStatus.ManyMatch:
                CountManyMatch++;
                break;
            case MatchStatus.NoMatch:
                CountNoMatch++;
                break;
            case MatchStatus.PotentialMatch:
                CountPotentialMatch++;
                break;
            case MatchStatus.LowConfidenceMatch:
                CountLowConfidenceMatch++;
                break;
            case MatchStatus.Error:
                ErroredCount++;
                break;
        }
    }

    public void RecordError()
    {
        Count++;
        ErroredCount++;
    }

    public void ResetStats()
    {
        Count = 0;
        ErroredCount = 0;
        CountMatched = 0;
        CountPotentialMatch = 0;
        CountLowConfidenceMatch = 0;
        CountManyMatch = 0;
        CountNoMatch = 0;
    }
}
