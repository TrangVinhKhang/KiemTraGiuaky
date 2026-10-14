using System.Collections.Generic;

namespace KiemTraGiuaKy.Exams.Dto;

public class QuestionDto
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public int QuestionNumber { get; set; }
    public string Content { get; set; }
    public string DifficultyLevel { get; set; } = "Trung bình";
    public string Level => DifficultyLevel;
    public double Score { get; set; } = 0.2;
    public string Tag { get; set; }
    public string ImageUrl { get; set; }
    public string Explanation { get; set; }
    public bool IsReviewed { get; set; }
    public string ReviewNotes { get; set; }
    public string Passage { get; set; }
    public string GroupHeader { get; set; }
    public bool HasSpellingWarning { get; set; }
    public List<QuestionAnswerDto> Answers { get; set; } = new List<QuestionAnswerDto>();
}
