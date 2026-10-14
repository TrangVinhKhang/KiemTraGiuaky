using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Abp.Application.Services;
using KiemTraGiuaKy.Exams.Dto;

namespace KiemTraGiuaKy.Exams;

public interface IExamAppService : IApplicationService
{
    Task<List<ExamDto>> GetAllExamsAsync();
    Task<ExamDto> GetExamByIdAsync(int id);
    Task<ExamParseResultDto> ImportWordExamAsync(Stream fileStream, string fileName, string title, string subject, string examCode, int durationMinutes);
    Task UpdateQuestionAsync(UpdateQuestionDto input);
    Task<bool> ApproveExamAsync(int examId);
    Task DeleteExamAsync(int id);
}
