using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using KiemTraGiuaKy.Exams.Dto;
using Microsoft.EntityFrameworkCore;

namespace KiemTraGiuaKy.Exams;

public class ExamAppService : ApplicationService, IExamAppService
{
    private readonly IRepository<Exam, int> _examRepository;
    private readonly IRepository<Question, int> _questionRepository;
    private readonly IRepository<QuestionAnswer, int> _questionAnswerRepository;

    public ExamAppService(
        IRepository<Exam, int> examRepository,
        IRepository<Question, int> questionRepository,
        IRepository<QuestionAnswer, int> questionAnswerRepository)
    {
        _examRepository = examRepository;
        _questionRepository = questionRepository;
        _questionAnswerRepository = questionAnswerRepository;
    }

    public async Task<List<ExamDto>> GetAllExamsAsync()
    {
        var exams = await _examRepository.GetAll()
            .Include(e => e.Questions)
            .OrderByDescending(e => e.CreationTime)
            .ToListAsync();

        return exams.Select(e => new ExamDto
        {
            Id = e.Id,
            Title = e.Title,
            ExamCode = e.ExamCode,
            Subject = e.Subject,
            DurationMinutes = e.DurationMinutes,
            TotalQuestions = e.TotalQuestions,
            OriginalFileName = e.OriginalFileName,
            Status = e.Status,
            Description = e.Description,
            CreationTime = e.CreationTime,
            ReviewedQuestionsCount = e.Questions.Count(q => q.IsReviewed),
            QuestionsWithImageCount = e.Questions.Count(q => !string.IsNullOrEmpty(q.ImageUrl))
        }).ToList();
    }

    public async Task<ExamDto> GetExamByIdAsync(int id)
    {
        var exam = await _examRepository.GetAll()
            .Include(e => e.Questions)
                .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exam == null) return null;

        var orderedQuestions = exam.Questions
            .OrderBy(q => q.QuestionNumber)
            .Select(q => new QuestionDto
            {
                Id = q.Id,
                ExamId = q.ExamId,
                QuestionNumber = q.QuestionNumber,
                Content = q.Content,
                DifficultyLevel = q.DifficultyLevel,
                Tag = q.Tag,
                ImageUrl = q.ImageUrl,
                Explanation = q.Explanation,
                IsReviewed = q.IsReviewed,
                ReviewNotes = q.ReviewNotes,
                Answers = q.Answers.OrderBy(a => a.OptionLabel).Select(a => new QuestionAnswerDto
                {
                    Id = a.Id,
                    QuestionId = a.QuestionId,
                    OptionLabel = a.OptionLabel,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            }).ToList();

        return new ExamDto
        {
            Id = exam.Id,
            Title = exam.Title,
            ExamCode = exam.ExamCode,
            Subject = exam.Subject,
            DurationMinutes = exam.DurationMinutes,
            TotalQuestions = exam.TotalQuestions,
            OriginalFileName = exam.OriginalFileName,
            Status = exam.Status,
            Description = exam.Description,
            CreationTime = exam.CreationTime,
            ReviewedQuestionsCount = exam.Questions.Count(q => q.IsReviewed),
            QuestionsWithImageCount = exam.Questions.Count(q => !string.IsNullOrEmpty(q.ImageUrl)),
            Questions = orderedQuestions
        };
    }

    public async Task<ExamParseResultDto> ImportWordExamAsync(
        Stream fileStream,
        string fileName,
        string title,
        string subject,
        string examCode,
        int durationMinutes)
    {
        var parser = new WordExamParserService();
        var parseResult = parser.ParseAndValidate(fileStream);

        // CHỈ HỦY NẾU HOÀN TOÀN KHÔNG TÌM THẤY CÂU HỎI HỢP LỆ HOẶC TỆP DEMO CỐ Ý TẠO LỖI
        if (parseResult.Questions.Count == 0 || !parseResult.IsValid)
        {
            parseResult.IsValid = false;
            return parseResult;
        }

        // LƯU CÁC CÂU HỎI ĐÃ TRÍCH XUẤT VÀO CSDL
        var exam = new Exam
        {
            Title = !string.IsNullOrWhiteSpace(title) ? title : (!string.IsNullOrWhiteSpace(parseResult.Title) ? parseResult.Title : Path.GetFileNameWithoutExtension(fileName)),
            Subject = !string.IsNullOrWhiteSpace(subject) ? subject : (!string.IsNullOrWhiteSpace(parseResult.Subject) ? parseResult.Subject : "Lập trình trên môi trường di động"),
            ExamCode = !string.IsNullOrWhiteSpace(examCode) ? examCode : (!string.IsNullOrWhiteSpace(parseResult.ExamCode) ? parseResult.ExamCode : "DE-" + DateTime.Now.ToString("yyyyMMddHHmm")),
            DurationMinutes = durationMinutes > 0 ? durationMinutes : (parseResult.DurationMinutes > 0 ? parseResult.DurationMinutes : 60),
            TotalQuestions = parseResult.Questions.Count,
            OriginalFileName = fileName,
            Status = parseResult.Errors.Count == 0 ? "100% Đạt chuẩn" : "Chờ rà soát",
            CreationTime = DateTime.Now
        };

        int examId = await _examRepository.InsertAndGetIdAsync(exam);

        foreach (var qDto in parseResult.Questions)
        {
            var question = new Question
            {
                ExamId = examId,
                QuestionNumber = qDto.QuestionNumber,
                Content = qDto.Content,
                DifficultyLevel = qDto.DifficultyLevel,
                Tag = qDto.Tag,
                ImageUrl = qDto.ImageUrl,
                Explanation = qDto.Explanation,
                IsReviewed = false,
                ReviewNotes = string.Empty
            };

            int questionId = await _questionRepository.InsertAndGetIdAsync(question);

            foreach (var aDto in qDto.Answers)
            {
                var answer = new QuestionAnswer
                {
                    QuestionId = questionId,
                    OptionLabel = aDto.OptionLabel,
                    Content = aDto.Content,
                    IsCorrect = aDto.IsCorrect
                };

                await _questionAnswerRepository.InsertAsync(answer);
            }
        }

        parseResult.SavedExamId = examId;
        parseResult.IsValid = true;
        if (parseResult.Errors.Count == 0)
        {
            parseResult.Message = $"Tải lên đề thi thành công! Đã lưu an toàn {parseResult.Questions.Count} câu hỏi chuẩn 100% vào CSDL.";
        }
        else
        {
            parseResult.Message = $"Đã tải lên và lưu thành công {parseResult.Questions.Count} câu hỏi vào CSDL! Có {parseResult.Errors.Count} câu hỏi có lưu ý cần rà soát tại Giao diện 2.";
        }
        return parseResult;
    }

    public async Task UpdateQuestionAsync(UpdateQuestionDto input)
    {
        var question = await _questionRepository.GetAll()
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == input.Id);

        if (question == null) return;

        question.Content = input.Content;
        question.DifficultyLevel = input.DifficultyLevel;
        question.Tag = input.Tag;
        question.Explanation = input.Explanation;
        question.IsReviewed = input.IsReviewed;
        question.ReviewNotes = input.ReviewNotes;
        if (!string.IsNullOrEmpty(input.ImageUrl))
        {
            question.ImageUrl = input.ImageUrl;
        }

        if (input.Answers != null && input.Answers.Any())
        {
            foreach (var inputAns in input.Answers)
            {
                var existing = question.Answers.FirstOrDefault(a => a.Id == inputAns.Id || a.OptionLabel == inputAns.OptionLabel);
                if (existing != null)
                {
                    existing.Content = inputAns.Content;
                    existing.IsCorrect = inputAns.IsCorrect;
                }
            }
        }

        await _questionRepository.UpdateAsync(question);
    }

    public async Task<bool> ApproveExamAsync(int examId)
    {
        var exam = await _examRepository.GetAsync(examId);
        if (exam == null) return false;

        exam.Status = "Đã duyệt";
        await _examRepository.UpdateAsync(exam);
        return true;
    }

    public async Task DeleteExamAsync(int id)
    {
        await _examRepository.DeleteAsync(id);
    }
}
