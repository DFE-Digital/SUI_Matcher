using System.Collections;

namespace Unit.Tests.Matching.ReconciliationServiceTests;

public class SuccessfulCasesTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        /*
           	Fields
           - NHS number
           - Given names
           - Family names
           - Birth date
           - Gender
           - Phone number
           - Email
           - Postcode
          
           	States for each:
           - Present local, missing NHS
           - Missing local, present NHS
           - Present both, different
           - Present both, same 
           
         */
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}