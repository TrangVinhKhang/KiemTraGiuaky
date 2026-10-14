namespace KiemTraGiuaKy.Exams.Dto;

public class ValidationErrorDto
{
    public int LineNumber { get; set; }
    public int QuestionNumber { get; set; }
    public string ErrorType { get; set; } // "Sai số thứ tự câu", "Thiếu đáp án", "Thiếu tag", "Nội dung rỗng"
    public string Severity { get; set; } = "Critical"; // "Critical", "Warning"
    public string ErrorTitle { get; set; }
    public string Message { get; set; }
    public string SuggestedFix { get; set; }
    public string RawContent { get; set; }
}
