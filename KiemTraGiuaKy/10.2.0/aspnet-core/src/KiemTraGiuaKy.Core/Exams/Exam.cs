using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace KiemTraGiuaKy.Exams;

[Table("AppExams")]
public class Exam : Entity<int>, IHasCreationTime
{
    [Required]
    [MaxLength(256)]
    public string Title { get; set; }

    [MaxLength(50)]
    public string ExamCode { get; set; }

    [MaxLength(150)]
    public string Subject { get; set; }

    public int DurationMinutes { get; set; }

    public int TotalQuestions { get; set; }

    [MaxLength(256)]
    public string OriginalFileName { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Chờ rà soát";

    public string Description { get; set; }

    public DateTime CreationTime { get; set; } = DateTime.Now;

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
