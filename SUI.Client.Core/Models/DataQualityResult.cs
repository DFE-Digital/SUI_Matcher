namespace SUI.Client.Core.Models;

public class DataQualityResult
{
    public QualityType Given { get; set; } = QualityType.Valid;
    public QualityType Family { get; set; } = QualityType.Valid;
    public QualityType Birthdate { get; set; } = QualityType.Valid;
    public QualityType AddressPostalCode { get; set; } = QualityType.Valid;
    public QualityType Phone { get; set; } = QualityType.Valid;
    public QualityType Email { get; set; } = QualityType.Valid;
    public QualityType Gender { get; set; } = QualityType.Valid;
}
