using System.Collections.Generic;

namespace KiemTraGiuaKy.Exams.Dto;

public class ExamParseResultDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; }
    public int TotalQuestions { get; set; }
    public int ValidQuestionsCount { get; set; }
    public int ImageCount { get; set; }
    public int EasyCount { get; set; }
    public int MediumCount { get; set; }
    public int HardCount { get; set; }

    public string Title { get; set; }
    public string ExamCode { get; set; }
    public string Subject { get; set; }
    public int DurationMinutes { get; set; }

    public int? SavedExamId { get; set; }

    public List<ValidationErrorDto> Errors { get; set; } = new List<ValidationErrorDto>();
    public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
}
