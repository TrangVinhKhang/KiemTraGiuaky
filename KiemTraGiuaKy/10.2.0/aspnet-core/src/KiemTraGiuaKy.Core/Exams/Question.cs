using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace KiemTraGiuaKy.Exams;

[Table("AppQuestions")]
public class Question : Entity<int>
{
    [ForeignKey(nameof(Exam))]
    public int ExamId { get; set; }

    public int QuestionNumber { get; set; }

    [Required]
    public string Content { get; set; }

    [MaxLength(50)]
    public string DifficultyLevel { get; set; } = "Trung bình";

    [MaxLength(100)]
    public string Tag { get; set; }

    [MaxLength(500)]
    public string ImageUrl { get; set; }

    public string Explanation { get; set; }

    public bool IsReviewed { get; set; } = false;

    [MaxLength(500)]
    public string ReviewNotes { get; set; }

    public virtual Exam Exam { get; set; }

    public virtual ICollection<QuestionAnswer> Answers { get; set; } = new List<QuestionAnswer>();
}
