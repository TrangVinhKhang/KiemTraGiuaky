using System;
using System.Collections.Generic;

namespace KiemTraGiuaKy.Exams.Dto;

public class ExamDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string ExamCode { get; set; }
    public string Subject { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalQuestions { get; set; }
    public string OriginalFileName { get; set; }
    public string Status { get; set; }
    public string Description { get; set; }
    public DateTime CreationTime { get; set; }
    public int ReviewedQuestionsCount { get; set; }
    public int QuestionsWithImageCount { get; set; }
    public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
}
