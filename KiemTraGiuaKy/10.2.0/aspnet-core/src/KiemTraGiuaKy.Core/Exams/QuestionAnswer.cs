using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;

namespace KiemTraGiuaKy.Exams;

[Table("AppQuestionAnswers")]
public class QuestionAnswer : Entity<int>
{
    [ForeignKey(nameof(Question))]
    public int QuestionId { get; set; }

    [Required]
    [MaxLength(10)]
    public string OptionLabel { get; set; } // A, B, C, D

    [Required]
    public string Content { get; set; }

    public bool IsCorrect { get; set; } = false;

    public virtual Question Question { get; set; }
}
