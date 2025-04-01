using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SUI.DBS.Client.Core.Models;

namespace SUI.DBS.Client.Core;

public interface ITxtFileProcessor
{
    Task ProcessFileAsync(string filePath);
}

public class TxtFileProcessor(ILogger<TxtFileProcessor> logger) : ITxtFileProcessor
{
    private static void AssertFileExists(string inputFilePath)
    {
        if (!File.Exists(inputFilePath))
        {
            throw new FileNotFoundException("File not found", inputFilePath);
        }
    }

    public async Task ProcessFileAsync(string filePath)
    {
        AssertFileExists(filePath);
        
        string[] lines = await File.ReadAllLinesAsync(filePath);

        var recordColumns = Enum.GetValues(typeof(RecordColumn)).Cast<RecordColumn>().ToArray();

        var currentRow = 0;
        var recordCount = 0;
        var matches = 0;
        var noMatches = 0;
        
        // Use foreach loop to process each line
        foreach (string line in lines)
        {
            var recordData = line.Replace("\"", "").Split(",");

            if (recordData.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                ++recordCount;
                
                var result = new MatchPersonResult();
                
                foreach (var recordColumn in recordColumns)
                {
                    var fieldName = recordColumn.ToString();
                    
                    var field = typeof(MatchPersonResult).GetField($"<{fieldName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field != null)
                    {
                        field.SetValue(result, recordData[(int) recordColumn]);
                    }
                }

                StoreUniqueSearchIdFor(result);

                var matched = !string.IsNullOrWhiteSpace(result.NhsNumber);

                if (matched)
                {
                    ++matches;
                }
                else
                {
                    ++noMatches;
                }
                
                logger.LogInformation($"The DBS search for record on line '{currentRow}' resulted in match status '{(matched ? "Match" : "NoMatch")}'");
            }
            
            ++currentRow;
        }
            
        logger.LogInformation($"The DBS results file has {recordCount} records, batch search resulted in Match='{matches}' and NoMatch='{noMatches}'");
    }
    
    private static void StoreUniqueSearchIdFor(MatchPersonResult matchPersonResult)
    {
        using var md5 = MD5.Create();
        byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(matchPersonResult));
        byte[] hashBytes = md5.ComputeHash(bytes);

        StringBuilder builder = new StringBuilder();
        
        foreach (var t in hashBytes)
        {
            builder.Append(t.ToString("x2"));
        }

        var hash = builder.ToString();

        Activity.Current?.SetTag("SearchId", hash);
    }

    
}
