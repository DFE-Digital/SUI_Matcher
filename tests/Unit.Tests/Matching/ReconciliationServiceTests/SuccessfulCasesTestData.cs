
using Shared.Models;

namespace Unit.Tests.Matching.ReconciliationServiceTests;

public record SuccessfulCase(
    string CaseLabel,
    ReconciliationRequest Request,
    string MatchedNhsNumber,
    NhsPerson NhsDemographicsForMatchedNhsNumber,
    NhsPerson NhsDemographicsForRequestNhsNumber,
    ReconciliationStatus ExpectedStatus,
    List<Difference> ExpectedDifferences)
{
    public override string ToString()
    {
        return CaseLabel;
    }
}

public class SuccessfulCasesTestData : TheoryData<SuccessfulCase>
{
    public SuccessfulCasesTestData()
    {
        Add(new SuccessfulCase(
            CaseLabel: "No differences on all fields",
            Request: DavidSmithAsReconciliationRequest(),
            MatchedNhsNumber: ReconcileAsyncTests.ValidNhsNumber,
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson(),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson(),
            ExpectedStatus: ReconciliationStatus.NoDifferences,
            ExpectedDifferences: []));

        /*
         * For each field reporting differences for, test for:
         * - Present in NHS, missing locally
         * - Missing locally, present in NHS
         * - Present in both, but with differences
         */

        #region NHS Number
        Add(new SuccessfulCase(
            CaseLabel: "NHS number present in NHS, missing locally",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.NhsNumber = null),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.NhsNumber = "9999999993"),
            NhsDemographicsForRequestNhsNumber: new NhsPerson() { NhsNumber = "" },
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.NhsNumber), Local = null, Nhs = "9999999993"}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "NHS number present locally and in NHS, but is different",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.NhsNumber = ReconcileAsyncTests.ValidNhsNumber),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.NhsNumber = "9999999993"),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.NhsNumber = ReconcileAsyncTests.ValidNhsNumber),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.NhsNumber), Local = ReconcileAsyncTests.ValidNhsNumber, Nhs = "9999999993"}
            ]));
        #endregion

        #region Given name
        Add(new SuccessfulCase(
            CaseLabel: "Given name present in NHS, missing locally",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Given = ""),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.GivenNames = ["David"]),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.GivenNames = ["David"]),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Given), Local = "", Nhs = "David"}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "Given name present locally, missing in NHS",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Given = "David"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.GivenNames = []),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.GivenNames = []),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Given), Local = "David", Nhs = ""}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "Given name present locally and in NHS, but is different",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Given = "David"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.GivenNames = ["Colin"]),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.GivenNames = ["Colin"]),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Given), Local = "David", Nhs = "Colin"}
            ]));
        #endregion

        #region Family name
        Add(new SuccessfulCase(
            CaseLabel: "Family name present in NHS, missing locally",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Family = ""),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.FamilyNames = ["Smith"]),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.FamilyNames = ["Smith"]),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Family), Local = "", Nhs = "Smith"}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "Family name present locally, missing in NHS",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Family = "Smith"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.FamilyNames = []),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.FamilyNames = []),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Family), Local = "Smith", Nhs = ""}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "Family name present locally and in NHS, but is different",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Family = "Smith"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.FamilyNames = ["Taylor"]),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.FamilyNames = ["Taylor"]),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences:
            [
                new Difference() { FieldName = nameof(ReconciliationRequest.Family), Local = "Smith", Nhs = "Taylor" }
            ]));
        #endregion

        #region Birth date
        Add(new SuccessfulCase(
           CaseLabel: "Birth date present in NHS, missing locally",
           Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.BirthDate = null),
           MatchedNhsNumber: "9999999993",
           NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.BirthDate = new DateOnly(2000,1,1)),
           NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.BirthDate = new DateOnly(2000,1,1)),
           ExpectedStatus: ReconciliationStatus.Differences,
           ExpectedDifferences: [
               new Difference() { FieldName = nameof(ReconciliationRequest.BirthDate), Local = null, Nhs = "2000-01-01"}
           ]));

        Add(new SuccessfulCase(
            CaseLabel: "Birth date present locally, missing in NHS",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.BirthDate = new DateOnly(2000,1,1)),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.BirthDate = null),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.BirthDate = null),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.BirthDate), Local = "2000-01-01", Nhs = null}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "Birth date present locally and in NHS, but is different",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.BirthDate = new DateOnly(2000,1,1)),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.BirthDate = new DateOnly(2013,1,22)),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.BirthDate = new DateOnly(2013,1,22)),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.BirthDate), Local = "2000-01-01", Nhs = "2013-01-22"}
            ]));
        #endregion
        
        #region Gender
        Add(new SuccessfulCase(
           CaseLabel: "Gender present in NHS, missing locally",
           Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Gender = null),
           MatchedNhsNumber: "9999999993",
           NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Gender = "Male"),
           NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Gender = "Male"),
           ExpectedStatus: ReconciliationStatus.Differences,
           ExpectedDifferences: [
               new Difference() { FieldName = nameof(ReconciliationRequest.Gender), Local = null, Nhs = "Male"}
           ]));

        Add(new SuccessfulCase(
            CaseLabel: "Gender present locally, missing in NHS",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Gender = "Male"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Gender = null),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Gender = null),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Gender), Local = "Male", Nhs = null}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "Gender present locally and in NHS, but is different",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Gender = "Male"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Gender = "Female"),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Gender = "Female"),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Gender), Local = "Male", Nhs = "Female"}
            ]));
        #endregion

        #region Phone number
        Add(new SuccessfulCase(
           CaseLabel: "Phone number present in NHS, missing locally",
           Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Phone = null),
           MatchedNhsNumber: "9999999993",
           NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.PhoneNumbers = [ "123454321" ]),
           NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.PhoneNumbers = [ "123454321" ]),
           ExpectedStatus: ReconciliationStatus.Differences,
           ExpectedDifferences: [
               new Difference() { FieldName = nameof(ReconciliationRequest.Phone), Local = null, Nhs = "123454321"}
           ]));

        Add(new SuccessfulCase(
            CaseLabel: "Phone number present locally, missing in NHS",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Phone = "123454321"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.PhoneNumbers = []),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.PhoneNumbers = []),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Phone), Local = "123454321", Nhs = ""}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "Phone number present locally and in NHS, but is different",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Phone = "123454321"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.PhoneNumbers = [ "543212345" ]),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.PhoneNumbers = [ "543212345" ]),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Phone), Local = "123454321", Nhs = "543212345"}
            ]));
        #endregion

        #region Email
        Add(new SuccessfulCase(
           CaseLabel: "Email present in NHS, missing locally",
           Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Email = null),
           MatchedNhsNumber: "9999999993",
           NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Emails = [ "david.smith@example.com" ]),
           NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Emails = [ "david.smith@example.com" ]),
           ExpectedStatus: ReconciliationStatus.Differences,
           ExpectedDifferences: [
               new Difference() { FieldName = nameof(ReconciliationRequest.Email), Local = null, Nhs = "david.smith@example.com"}
           ]));

        Add(new SuccessfulCase(
            CaseLabel: "Email present locally, missing in NHS",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Email = "david.smith@example.com"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Emails = []),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Emails = []),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Email), Local = "david.smith@example.com", Nhs = ""}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "Email present locally and in NHS, but is different",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Email = "david.smith@example.com"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Emails = [ "david.h.smith@aol.net" ]),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.Emails = [ "david.h.smith@aol.net" ]),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.Email), Local = "david.smith@example.com", Nhs = "david.h.smith@aol.net"}
            ]));
        #endregion

        #region Postcode
        Add(new SuccessfulCase(
           CaseLabel: "Postcode present in NHS, missing locally",
           Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.AddressPostalCode = null),
           MatchedNhsNumber: "9999999993",
           NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.AddressPostalCodes = [ "L1 8JQ" ]),
           NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.AddressPostalCodes = [ "L1 8JQ" ]),
           ExpectedStatus: ReconciliationStatus.Differences,
           ExpectedDifferences: [
               new Difference() { FieldName = nameof(ReconciliationRequest.AddressPostalCode), Local = null, Nhs = "L1 8JQ"}
           ]));

        Add(new SuccessfulCase(
            CaseLabel: "Postcode present locally, missing in NHS",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.AddressPostalCode = "L1 8JQ"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.AddressPostalCodes = []),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.AddressPostalCodes = []),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.AddressPostalCode), Local = "L1 8JQ", Nhs = ""}
            ]));

        Add(new SuccessfulCase(
            CaseLabel: "Postcode present locally and in NHS, but is different",
            Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.AddressPostalCode = "L1 8JQ"),
            MatchedNhsNumber: "9999999993",
            NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.AddressPostalCodes = [ "M1 1AA" ]),
            NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson().Configure(np => np.AddressPostalCodes = [ "M1 1AA" ]),
            ExpectedStatus: ReconciliationStatus.Differences,
            ExpectedDifferences: [
                new Difference() { FieldName = nameof(ReconciliationRequest.AddressPostalCode), Local = "L1 8JQ", Nhs = "M1 1AA"}
            ]));
        #endregion
    }

    private static ReconciliationRequest DavidSmithAsReconciliationRequest() => new()
    {
        NhsNumber = ReconcileAsyncTests.ValidNhsNumber,
        AddressPostalCode = "AA11 2BB",
        Family = "Smith",
        Given = "David",
        BirthDate = new DateOnly(1980, 1, 1),
        Gender = "Male",
        Phone = "123454321",
        Email = "david.smith@example.com",
    };

    private static NhsPerson DavidSmithAsNhsPerson() => new()
    {
        NhsNumber = ReconcileAsyncTests.ValidNhsNumber,
        AddressPostalCodes = ["AA11 2BB"],
        FamilyNames = ["Smith"],
        GivenNames = ["David"],
        BirthDate = new DateOnly(1980, 1, 1),
        Gender = "Male",
        PhoneNumbers = ["123454321"],
        Emails = ["david.smith@example.com"],
    };
}

public static class SuccessfulCasesTestDataExtensions
{
    public static T Configure<T>(this T target, Action<T> configure)
    {
        configure(target);
        return target;
    }
}