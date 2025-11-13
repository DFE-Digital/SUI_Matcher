using SUI.Client.Core.Models;

namespace SUI.Client.Core;

public class MatchingCsvProcessStats : IStats
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

    public MatchingCsvProcessStats()
    {
        _erroredPercentage = new Lazy<double>(() => ComputePercentage(ErroredCount));
        _matchedPercentage = new Lazy<double>(() => ComputePercentage(CountMatched));
        _potentialMatchPercentage = new Lazy<double>(() => ComputePercentage(CountPotentialMatch));
        _lowConfidenceMatchPercentage = new Lazy<double>(() => ComputePercentage(CountLowConfidenceMatch));
        _manyMatchPercentage = new Lazy<double>(() => ComputePercentage(CountManyMatch));
        _noMatchPercentage = new Lazy<double>(() => ComputePercentage(CountNoMatch));
    }

    private double ComputePercentage(int count) => Count == 0 ? 0 : Math.Round((double)count / Count * 100, 2);

    public double ErroredPercentage => _erroredPercentage.Value;
    public double MatchedPercentage => _matchedPercentage.Value;
    public double PotentialMatchPercentage => _potentialMatchPercentage.Value;
    public double LowConfidenceMatchPercentage => _lowConfidenceMatchPercentage.Value;
    public double ManyMatchPercentage => _manyMatchPercentage.Value;
    public double NoMatchPercentage => _noMatchPercentage.Value;
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