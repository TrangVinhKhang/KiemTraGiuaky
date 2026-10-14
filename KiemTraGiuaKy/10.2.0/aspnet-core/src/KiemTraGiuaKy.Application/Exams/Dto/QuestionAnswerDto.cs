namespace KiemTraGiuaKy.Exams.Dto;

public class QuestionAnswerDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string OptionLabel { get; set; }
    public string Content { get; set; }
    public bool IsCorrect { get; set; }
}
