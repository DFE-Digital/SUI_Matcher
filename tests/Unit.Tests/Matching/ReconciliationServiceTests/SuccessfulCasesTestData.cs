using Shared.Models;

namespace Unit.Tests.Matching.ReconciliationServiceTests;

public record SuccessfulCase(
    string CaseLabel,
    ReconciliationRequest Request,
    string MatchedNhsNumber,
    NhsPerson NhsDemographicsForMatchedNhsNumber,
    NhsPerson NhsDemographicsForRequestNhsNumber,
    ReconciliationStatus ExpectedStatus,
    List<string> ExpectedDifferences,
    List<string> ExpectedMissingLocalFields,
    List<string> ExpectedMissingNhsFields
)
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
        AddNoDifferencesCase();
        AddNhsNumberCases();
        AddGivenNameCases();
        AddFamilyNameCases();
        AddBirthDateCases();
        AddGenderCases();
        AddPhoneNumberCases();
        AddEmailCases();
        AddPostcodeCases();
    }

    private void AddNoDifferencesCase()
    {
        Add(
            new SuccessfulCase(
                CaseLabel: "No differences on all fields",
                Request: DavidSmithAsReconciliationRequest(),
                MatchedNhsNumber: ReconcileAsyncTests.ValidNhsNumber,
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson(),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson(),
                ExpectedStatus: ReconciliationStatus.NoDifferences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: []
            )
        );
    }

    private void AddNhsNumberCases()
    {
        Add(
            new SuccessfulCase(
                CaseLabel: "NHS number present in NHS, missing locally",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.NhsNumber = null),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.NhsNumber = "9999999993"),
                NhsDemographicsForRequestNhsNumber: new NhsPerson() { NhsNumber = "" },
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [nameof(ReconciliationRequest.NhsNumber)],
                ExpectedMissingNhsFields: []
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "NHS number present locally and in NHS, but is different",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.NhsNumber = ReconcileAsyncTests.ValidNhsNumber),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.NhsNumber = "9999999993"),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.NhsNumber = ReconcileAsyncTests.ValidNhsNumber),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [nameof(ReconciliationRequest.NhsNumber)],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: []
            )
        );
    }

    private void AddGivenNameCases()
    {
        Add(
            new SuccessfulCase(
                CaseLabel: "Given name present in NHS, missing locally",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Given = ""),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.GivenNames = ["David"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.GivenNames = ["David"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [nameof(ReconciliationRequest.Given)],
                ExpectedMissingNhsFields: []
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Given name present locally, missing in NHS",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Given = "David"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.GivenNames = []),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.GivenNames = []),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: [nameof(ReconciliationRequest.Given)]
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Given name present locally and in NHS, but is different",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Given = "David"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.GivenNames = ["Colin"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.GivenNames = ["Colin"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [nameof(ReconciliationRequest.Given)],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: []
            )
        );
    }

    private void AddFamilyNameCases()
    {
        Add(
            new SuccessfulCase(
                CaseLabel: "Family name present in NHS, missing locally",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Family = ""),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.FamilyNames = ["Smith"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.FamilyNames = ["Smith"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [nameof(ReconciliationRequest.Family)],
                ExpectedMissingNhsFields: []
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Family name present locally, missing in NHS",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Family = "Smith"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.FamilyNames = []),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.FamilyNames = []),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: [nameof(ReconciliationRequest.Family)]
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Family name present locally and in NHS, but is different",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Family = "Smith"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.FamilyNames = ["Taylor"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.FamilyNames = ["Taylor"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [nameof(ReconciliationRequest.Family)],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: []
            )
        );
    }

    private void AddBirthDateCases()
    {
        Add(
            new SuccessfulCase(
                CaseLabel: "Birth date present in NHS, missing locally",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.BirthDate = null),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.BirthDate = new DateOnly(2000, 1, 1)),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.BirthDate = new DateOnly(2000, 1, 1)),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [nameof(ReconciliationRequest.BirthDate)],
                ExpectedMissingNhsFields: []
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Birth date present locally, missing in NHS",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.BirthDate = new DateOnly(2000, 1, 1)),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.BirthDate = null),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.BirthDate = null),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: [nameof(ReconciliationRequest.BirthDate)]
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Birth date present locally and in NHS, but is different",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.BirthDate = new DateOnly(2000, 1, 1)),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.BirthDate = new DateOnly(2013, 1, 22)),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.BirthDate = new DateOnly(2013, 1, 22)),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [nameof(ReconciliationRequest.BirthDate)],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: []
            )
        );
    }

    private void AddGenderCases()
    {
        Add(
            new SuccessfulCase(
                CaseLabel: "Gender present in NHS, missing locally",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Gender = null),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Gender = "Male"),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Gender = "Male"),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [nameof(ReconciliationRequest.Gender)],
                ExpectedMissingNhsFields: []
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Gender present locally, missing in NHS",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Gender = "Male"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Gender = null),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Gender = null),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: [nameof(ReconciliationRequest.Gender)]
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Gender present locally and in NHS, but is different",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Gender = "Male"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Gender = "Female"),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Gender = "Female"),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [nameof(ReconciliationRequest.Gender)],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: []
            )
        );
    }

    private void AddPhoneNumberCases()
    {
        Add(
            new SuccessfulCase(
                CaseLabel: "Phone number present in NHS, missing locally",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Phone = null),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.PhoneNumbers = ["123454321"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.PhoneNumbers = ["123454321"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [nameof(ReconciliationRequest.Phone)],
                ExpectedMissingNhsFields: []
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Phone number present locally, missing in NHS",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.Phone = "123454321"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.PhoneNumbers = []),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.PhoneNumbers = []),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: [nameof(ReconciliationRequest.Phone)]
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Phone number present locally and in NHS, but is different",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.Phone = "123454321"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.PhoneNumbers = ["543212345"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.PhoneNumbers = ["543212345"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [nameof(ReconciliationRequest.Phone)],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: []
            )
        );
    }

    private void AddEmailCases()
    {
        Add(
            new SuccessfulCase(
                CaseLabel: "Email present in NHS, missing locally",
                Request: DavidSmithAsReconciliationRequest().Configure(rr => rr.Email = null),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Emails = ["david.smith@example.com"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Emails = ["david.smith@example.com"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [nameof(ReconciliationRequest.Email)],
                ExpectedMissingNhsFields: []
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Email present locally, missing in NHS",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.Email = "david.smith@example.com"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Emails = []),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Emails = []),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: [nameof(ReconciliationRequest.Email)]
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Email present locally and in NHS, but is different",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.Email = "david.smith@example.com"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Emails = ["david.h.smith@aol.net"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.Emails = ["david.h.smith@aol.net"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [nameof(ReconciliationRequest.Email)],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: []
            )
        );
    }

    private void AddPostcodeCases()
    {
        Add(
            new SuccessfulCase(
                CaseLabel: "Postcode present in NHS, missing locally",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.AddressPostalCode = null),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.AddressPostalCodes = ["L1 8JQ"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.AddressPostalCodes = ["L1 8JQ"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [nameof(ReconciliationRequest.AddressPostalCode)],
                ExpectedMissingNhsFields: []
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Postcode present locally, missing in NHS",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.AddressPostalCode = "L1 8JQ"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.AddressPostalCodes = []),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.AddressPostalCodes = []),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: [nameof(ReconciliationRequest.AddressPostalCode)]
            )
        );

        Add(
            new SuccessfulCase(
                CaseLabel: "Postcode present locally and in NHS, but is different",
                Request: DavidSmithAsReconciliationRequest()
                    .Configure(rr => rr.AddressPostalCode = "L1 8JQ"),
                MatchedNhsNumber: "9999999993",
                NhsDemographicsForMatchedNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.AddressPostalCodes = ["M1 1AA"]),
                NhsDemographicsForRequestNhsNumber: DavidSmithAsNhsPerson()
                    .Configure(np => np.AddressPostalCodes = ["M1 1AA"]),
                ExpectedStatus: ReconciliationStatus.Differences,
                ExpectedDifferences: [nameof(ReconciliationRequest.AddressPostalCode)],
                ExpectedMissingLocalFields: [],
                ExpectedMissingNhsFields: []
            )
        );
    }

    private static ReconciliationRequest DavidSmithAsReconciliationRequest() =>
        new()
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

    private static NhsPerson DavidSmithAsNhsPerson() =>
        new()
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
