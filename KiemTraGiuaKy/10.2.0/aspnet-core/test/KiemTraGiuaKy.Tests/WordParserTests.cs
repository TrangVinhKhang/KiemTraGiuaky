using System;
using System.IO;
using System.Linq;
using KiemTraGiuaKy.Exams;
using Xunit;

namespace KiemTraGiuaKy.Tests;

public class WordParserTests
{
    [Fact]
    public void TestStandardWordDocx()
    {
        var parser = new WordExamParserService();
        var bytes = WordSampleGenerator.GenerateStandardSampleDocx();
        using var stream = new MemoryStream(bytes);
        var result = parser.ParseAndValidate(stream);
        
        foreach (var err in result.Errors)
        {
            Console.WriteLine($"[ERR] L{err.LineNumber} Q{err.QuestionNumber}: {err.ErrorType} - {err.Message}");
        }

        Assert.True(result.IsValid, $"Standard docx should be valid, but had {result.Errors.Count} errors: {result.Message}");
    }

    [Fact]
    public void TestToeicWordDocx()
    {
        var parser = new WordExamParserService();
        var bytes = WordSampleGenerator.GenerateToeicSampleDocx();
        using var stream = new MemoryStream(bytes);
        var result = parser.ParseAndValidate(stream);
        
        foreach (var err in result.Errors)
        {
            Console.WriteLine($"[ERR] L{err.LineNumber} Q{err.QuestionNumber}: {err.ErrorType} - {err.Message}");
        }

        string allErrors = string.Join("\n", result.Errors.Select(e => $"Q{e.QuestionNumber}: {e.ErrorTitle} - {e.Message}"));
        Assert.True(result.IsValid, $"Toeic docx should be valid, but had {result.Errors.Count} errors:\n{allErrors}");
    }
}
