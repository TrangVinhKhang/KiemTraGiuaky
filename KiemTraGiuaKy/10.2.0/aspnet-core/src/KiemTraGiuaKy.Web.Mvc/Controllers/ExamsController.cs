using System;
using System.IO;
using System.Threading.Tasks;
using KiemTraGiuaKy.Controllers;
using KiemTraGiuaKy.Exams;
using KiemTraGiuaKy.Exams.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KiemTraGiuaKy.Web.Controllers;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class ExamsController : KiemTraGiuaKyControllerBase
{
    private readonly IExamAppService _examAppService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ExamsController(
        IExamAppService examAppService,
        IWebHostEnvironment webHostEnvironment)
    {
        _examAppService = examAppService;
        _webHostEnvironment = webHostEnvironment;
    }

    // Ngân hàng đề thi: Hiển thị tất cả các đề thi đã import từ file Word
    public async Task<IActionResult> Index()
    {
        var exams = await _examAppService.GetAllExamsAsync();
        return View(exams);
    }

    // Giao diện 1: Tạo đề thi từ file WORD & Thẩm định chuẩn 100%
    public IActionResult Create()
    {
        return View();
    }

    // Xử lý upload file Word .docx và kiểm tra định dạng
    [HttpPost]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
    public async Task<IActionResult> UploadWord(
        IFormFile file,
        string title,
        string subject,
        string examCode,
        int durationMinutes = 60)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new
            {
                success = false,
                message = "Vui lòng chọn một tệp tin Word (.docx) để tải lên."
            });
        }

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".docx" && ext != ".doc")
        {
            return Json(new
            {
                success = false,
                message = "Định dạng tệp không hợp lệ! Hệ thống hỗ trợ tệp Microsoft Word (.docx / .doc)."
            });
        }

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var result = await _examAppService.ImportWordExamAsync(
                memoryStream,
                file.FileName,
                title,
                subject,
                examCode,
                durationMinutes);

            return Json(new
            {
                success = result.IsValid,
                message = result.Message,
                data = result
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"Đã xảy ra lỗi hệ thống trong quá trình phân tích file: {ex.Message}"
            });
        }
    }

    // Giao diện 2: Xem trước đề thi, rà soát lỗi chính tả & rà soát hình ảnh
    public async Task<IActionResult> Preview(int id)
    {
        var exam = await _examAppService.GetExamByIdAsync(id);
        if (exam == null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(exam);
    }

    // API Cập nhật câu hỏi sau khi Giảng viên rà soát lỗi chính tả / ảnh
    [HttpPost]
    public async Task<IActionResult> UpdateQuestion([FromBody] UpdateQuestionDto input)
    {
        if (input == null || input.Id <= 0)
        {
            return Json(new { success = false, message = "Dữ liệu cập nhật không hợp lệ." });
        }

        try
        {
            await _examAppService.UpdateQuestionAsync(input);
            return Json(new { success = true, message = "Đã lưu chỉnh sửa câu hỏi thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Lỗi khi lưu: {ex.Message}" });
        }
    }

    // Phê duyệt đề thi
    [HttpPost]
    public async Task<IActionResult> ApproveExam(int id)
    {
        try
        {
            var success = await _examAppService.ApproveExamAsync(id);
            return Json(new { success, message = success ? "Đề thi đã được phê duyệt chính thức!" : "Không tìm thấy đề thi." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // Xóa đề thi
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _examAppService.DeleteExamAsync(id);
            return Json(new { success = true, message = "Đã xóa đề thi thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // API Nạp mẫu thử nghiệm trực tiếp vào CSDL để test nhanh (Chuẩn 100% hoặc TOEIC)
    [HttpPost]
    public async Task<IActionResult> ImportSampleExam([FromQuery] string type = "standard")
    {
        try
        {
            byte[] fileBytes;
            string fileName;
            string title;
            string subject;

            if (type == "toeic")
            {
                fileBytes = WordSampleGenerator.GenerateToeicSampleDocx();
                fileName = "TOEIC_Reading_Test_1.docx";
                title = "TOEIC Reading Test 1 (Part 5)";
                subject = "Tiếng Anh TOEIC";
            }
            else if (type == "error")
            {
                fileBytes = WordSampleGenerator.GenerateErrorSampleDocx();
                fileName = "DeThi_Mau_Co_Loi_Format.docx";
                title = "Đề Thi Thử Nghiệm Có Lỗi Cần Rà Soát";
                subject = "Tiếng Anh Khảo Sát";
            }
            else
            {
                fileBytes = WordSampleGenerator.GenerateStandardSampleDocx();
                fileName = "DeThi_Mau_Chuan_100.docx";
                title = "Đề Khảo Sát Năng Lực Tiếng Anh THPT - Mã đề ENG-2025";
                subject = "Tiếng Anh TOEIC";
            }

            using var memoryStream = new MemoryStream(fileBytes);
            var result = await _examAppService.ImportWordExamAsync(
                memoryStream,
                fileName,
                title,
                subject,
                type == "toeic" ? "TOEIC-2025" : (type == "error" ? "ERR-TEST" : "ENG-2025"),
                75);

            return Json(new
            {
                success = result.IsValid,
                message = result.Message,
                data = result
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"Lỗi khi nạp mẫu thử nghiệm: {ex.Message}"
            });
        }
    }

    // Tải file mẫu Word: Chuẩn 100%, TOEIC hoặc Mẫu cố ý có lỗi để test
    public IActionResult DownloadSample(string type = "standard")
    {
        byte[] fileBytes;
        string fileName;

        if (type == "error")
        {
            fileBytes = WordSampleGenerator.GenerateErrorSampleDocx();
            fileName = "DeThi_Mau_Co_Loi_Format.docx";
        }
        else if (type == "toeic")
        {
            fileBytes = WordSampleGenerator.GenerateToeicSampleDocx();
            fileName = "TOEIC_Reading_Test_1.docx";
        }
        else
        {
            fileBytes = WordSampleGenerator.GenerateStandardSampleDocx();
            fileName = "DeThi_Mau_Chuan_100.docx";
        }

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileName);
    }
}
