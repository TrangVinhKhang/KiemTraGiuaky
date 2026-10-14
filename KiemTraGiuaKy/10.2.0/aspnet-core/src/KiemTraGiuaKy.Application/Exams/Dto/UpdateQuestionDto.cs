using System.Collections.Generic;

namespace KiemTraGiuaKy.Exams.Dto;

public class UpdateQuestionDto
{
    public int Id { get; set; }
    public string Content { get; set; }
    public string DifficultyLevel { get; set; }
    public string Tag { get; set; }
    public string Explanation { get; set; }
    public bool IsReviewed { get; set; }
    public string ReviewNotes { get; set; }
    public string ImageUrl { get; set; }
    public List<QuestionAnswerDto> Answers { get; set; } = new List<QuestionAnswerDto>();
}
