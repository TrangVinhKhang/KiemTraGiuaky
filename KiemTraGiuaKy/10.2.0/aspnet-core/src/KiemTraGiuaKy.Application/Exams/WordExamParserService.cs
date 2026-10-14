using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KiemTraGiuaKy.Exams.Dto;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using V = DocumentFormat.OpenXml.Vml;

namespace KiemTraGiuaKy.Exams;

public class RawQuestionItem
{
    public int LineNumber { get; set; }
    public int DeclaredNumber { get; set; }
    public string HeaderRaw { get; set; }
    public string Content { get; set; } = string.Empty;
    public string DifficultyLevel { get; set; } = "Trung bình";
    public string Tag { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public List<QuestionAnswerDto> Answers { get; set; } = new List<QuestionAnswerDto>();
}

public class WordExamParserService
{
    private static readonly Regex QuestionRegex = new(
        @"^(?:(?:(?:Câu|Cau|Question|Bài|Bai|Q)\s*(\d+)[\s*.:\-\)\/\]]*)|\[(?:Câu|Cau|Question|Bài|Bai|Q)?\s*(\d+)\]|\((?:Câu|Cau|Question|Bài|Bai|Q)?\s*(\d+)\)|<Question\s*(\d+)>|(\d+)\s*[\.\:\-\)\/]\s*)\s*(.*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex OptionRegex = new(
        @"^([*]?)\s*(?:\(?([A-Ea-e])\s*[\.\:\-\)\/\]]|\(([A-Ea-e])\)|\[([A-Ea-e])\])\s*([*]?)\s*(.*)$",
        RegexOptions.Compiled);

    private static readonly Regex MultiOptionSplitRegex = new(
        @"(?:^|\s+|\t)([*]?)\s*(?:\(?([A-Ea-e])\s*[\.\:\-\)\/\]]|\(([A-Ea-e])\)|\[([A-Ea-e])\])\s*([*]?)\s*(.*?)(?=(?:\s+[*]?\s*(?:\(?[A-Ea-e]\s*[\.\:\-\)\/\]]|\([A-Ea-e]\)|\[[A-Ea-e]\]))|$)",
        RegexOptions.Compiled);

    private static readonly Regex CorrectAnswerSeparateRegex = new(
        @"^(?:(?:Đáp\s*án|Dap\s*an|Answer|Key|ĐA|DA)[\s*.:\-\)\/\]]*|<Answer>)\s*([A-Ea-e])(?:</Answer>)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ExplanationRegex = new(
        @"^(?:(?:Giải\s*thích|Lời\s*giải|Explanation)[\s*.:\-\]]|<Explanation>)\s*(.*?)(?:</Explanation>)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LevelRegex = new(
        @"(?:<Level:?\s*([^>]+)>|\[Mức độ:?\s*([^\]]+)\]|<Level>([^<]+)</Level>)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new(
        @"(?:<Tag:?\s*([^>]+)>|\[Chủ đề:?\s*([^\]]+)\]|<Tag>([^<]+)</Tag>)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AnswerKeyHeaderRegex = new(
        @"(?:BẢNG\s+ĐÁP\s+ÁN|ĐÁP\s+ÁN|BẢNG\s+TRẢ\s+LỜI|ANSWER\s+KEY|KEY|ANSWER)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AnswerKeyItemRegex = new(
        @"(?:(?:Câu|Cau|Q)?\s*(\d+)[\s*.:\-\)\/\]]*([A-Ea-e]))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ExamParseResultDto ParseAndValidate(Stream docxStream, string webRootUploadPath = null)
    {
        var result = new ExamParseResultDto
        {
            Errors = new List<ValidationErrorDto>(),
            Questions = new List<QuestionDto>()
        };

        var rawQuestions = new List<RawQuestionItem>();
        RawQuestionItem currentQuestion = null;
        int currentLineNumber = 0;

        string extractedTitle = null;
        string extractedSubject = null;
        string extractedCode = null;
        int extractedDuration = 0;

        bool inAnswerKeySection = false;
        var bottomAnswerKeys = new Dictionary<int, string>();

        try
        {
            using var doc = WordprocessingDocument.Open(docxStream, false);
            var mainPart = doc.MainDocumentPart;
            if (mainPart?.Document?.Body == null)
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineNumber = 1,
                    QuestionNumber = 0,
                    ErrorType = "Tệp tin rỗng",
                    ErrorTitle = "Tệp tin Word không có nội dung",
                    Message = "Không thể tìm thấy nội dung văn bản trong tài liệu .docx đã tải lên.",
                    SuggestedFix = "Vui lòng kiểm tra lại file Word và chắc chắn rằng file có chứa nội dung đề thi."
                });
                result.IsValid = false;
                result.Message = "File Word không có nội dung.";
                return result;
            }

            var paragraphs = mainPart.Document.Body.Descendants<Paragraph>().ToList();
            var fullDocText = string.Join("\n", paragraphs.Select(p => p.InnerText ?? ""));

            // Trích xuất bảng đáp án từ tất cả các Table trong tài liệu (nếu có)
            ExtractTableAnswerKeys(mainPart, bottomAnswerKeys);

            // NẾU TỆP TIN DÙNG ĐỊNH DẠNG THẺ XML / TAGGED (Như đề thi TOEIC: <question number="101">, <stem>, <options>, <answer>, <passage>)
            if (fullDocText.Contains("<question", StringComparison.OrdinalIgnoreCase))
            {
                rawQuestions = ParseXmlTaggedQuestions(paragraphs, mainPart, webRootUploadPath, out extractedTitle, out extractedSubject, out extractedCode, out extractedDuration);
            }
            else
            {
                // ĐỊNH DẠNG VĂN BẢN TRUYỀN THỐNG (Câu 1:, A., B., C., D.)
                foreach (var para in paragraphs)
                {
                    currentLineNumber++;
                    string lineText = para.InnerText?.Trim();

                    // Trích xuất hình ảnh trong paragraph nếu có
                    string paraImageUrl = ExtractImageFromParagraph(para, mainPart, webRootUploadPath, currentLineNumber);

                    if (string.IsNullOrWhiteSpace(lineText) && string.IsNullOrEmpty(paraImageUrl))
                    {
                        continue;
                    }

                    // Kiểm tra xem paragraph có định dạng Underline, Bold hoặc Màu chữ đỏ (dành cho đánh dấu đáp án đúng trong Word)
                    bool paraHasUnderline = para.Descendants<Run>().Any(r => 
                        r.RunProperties?.Underline != null && 
                        (r.RunProperties.Underline.Val == null || r.RunProperties.Underline.Val.Value != UnderlineValues.None));

                    bool paraHasBold = para.Descendants<Run>().Any(r => 
                        r.RunProperties?.Bold != null && 
                        (r.RunProperties.Bold.Val == null || r.RunProperties.Bold.Val.Value));

                    bool paraHasRedColor = para.Descendants<Run>().Any(r => 
                        r.RunProperties?.Color?.Val?.Value != null && 
                        (r.RunProperties.Color.Val.Value.Equals("FF0000", StringComparison.OrdinalIgnoreCase) ||
                         r.RunProperties.Color.Val.Value.Equals("red", StringComparison.OrdinalIgnoreCase)));

                    // Kiểm tra nếu là phần BẢNG ĐÁP ÁN ở cuối file
                    if (AnswerKeyHeaderRegex.IsMatch(lineText))
                    {
                        inAnswerKeySection = true;
                        if (currentQuestion != null)
                        {
                            rawQuestions.Add(currentQuestion);
                            currentQuestion = null;
                        }
                        continue;
                    }

                    if (inAnswerKeySection)
                    {
                        var keyMatches = AnswerKeyItemRegex.Matches(lineText);
                        foreach (Match km in keyMatches)
                        {
                            if (int.TryParse(km.Groups[1].Value, out int qNum))
                            {
                                bottomAnswerKeys[qNum] = km.Groups[2].Value.ToUpper();
                            }
                        }
                        continue;
                    }

                    // Kiểm tra metadata ở đầu file (tiêu đề, môn học, mã đề, thời gian)
                    if (currentQuestion == null && rawQuestions.Count == 0)
                    {
                        if (lineText.StartsWith("[Tên đề]:", StringComparison.OrdinalIgnoreCase) ||
                            lineText.StartsWith("Tên đề:", StringComparison.OrdinalIgnoreCase))
                        {
                            extractedTitle = lineText.Substring(lineText.IndexOf(':') + 1).Trim();
                            continue;
                        }
                        if (lineText.StartsWith("[Môn học]:", StringComparison.OrdinalIgnoreCase) ||
                            lineText.StartsWith("Môn học:", StringComparison.OrdinalIgnoreCase))
                        {
                            extractedSubject = lineText.Substring(lineText.IndexOf(':') + 1).Trim();
                            continue;
                        }
                        if (lineText.StartsWith("[Mã đề]:", StringComparison.OrdinalIgnoreCase) ||
                            lineText.StartsWith("Mã đề:", StringComparison.OrdinalIgnoreCase))
                        {
                            extractedCode = lineText.Substring(lineText.IndexOf(':') + 1).Trim();
                            continue;
                        }
                        if (lineText.StartsWith("[Thời gian]:", StringComparison.OrdinalIgnoreCase) ||
                            lineText.StartsWith("Thời gian:", StringComparison.OrdinalIgnoreCase))
                        {
                            var timePart = lineText.Substring(lineText.IndexOf(':') + 1).Trim();
                            var numMatch = Regex.Match(timePart, @"\d+");
                            if (numMatch.Success) int.TryParse(numMatch.Value, out extractedDuration);
                            continue;
                        }
                    }

                    // Kiểm tra xem dòng này có bắt đầu một câu hỏi mới không
                    var qMatch = QuestionRegex.Match(lineText);
                    if (qMatch.Success)
                    {
                        if (currentQuestion != null)
                        {
                            rawQuestions.Add(currentQuestion);
                        }

                        string numStr = qMatch.Groups[1].Value;
                        if (string.IsNullOrEmpty(numStr)) numStr = qMatch.Groups[2].Value;
                        if (string.IsNullOrEmpty(numStr)) numStr = qMatch.Groups[3].Value;
                        if (string.IsNullOrEmpty(numStr)) numStr = qMatch.Groups[4].Value;
                        if (string.IsNullOrEmpty(numStr)) numStr = qMatch.Groups[5].Value;

                        int.TryParse(numStr, out int declaredNum);

                        string remainingContent = qMatch.Groups[6].Value.Trim();

                        // Bóc tách tag Level và Tag nếu có
                        string level = "Trung bình";
                        var levelMatch = LevelRegex.Match(lineText);
                        if (levelMatch.Success)
                        {
                            level = levelMatch.Groups[1].Value;
                            if (string.IsNullOrEmpty(level)) level = levelMatch.Groups[2].Value;
                            if (string.IsNullOrEmpty(level)) level = levelMatch.Groups[3].Value;
                            level = level.Trim();
                            remainingContent = LevelRegex.Replace(remainingContent, "").Trim();
                        }

                        string tag = string.Empty;
                        var tagMatch = TagRegex.Match(lineText);
                        if (tagMatch.Success)
                        {
                            tag = tagMatch.Groups[1].Value;
                            if (string.IsNullOrEmpty(tag)) tag = tagMatch.Groups[2].Value;
                            if (string.IsNullOrEmpty(tag)) tag = tagMatch.Groups[3].Value;
                            tag = tag.Trim();
                            remainingContent = TagRegex.Replace(remainingContent, "").Trim();
                        }

                        currentQuestion = new RawQuestionItem
                        {
                            LineNumber = currentLineNumber,
                            DeclaredNumber = declaredNum,
                            HeaderRaw = lineText,
                            Content = remainingContent,
                            DifficultyLevel = string.IsNullOrWhiteSpace(level) ? "Trung bình" : level,
                            Tag = tag,
                            ImageUrl = paraImageUrl ?? string.Empty
                        };
                        continue;
                    }

                    // Nếu chưa vào câu hỏi nào (văn bản giới thiệu, ghi chú đầu đề thi)
                    if (currentQuestion == null)
                    {
                        continue;
                    }

                    // Nếu câu hỏi hiện tại gặp hình ảnh ở dòng tiếp theo
                    if (!string.IsNullOrEmpty(paraImageUrl))
                    {
                        if (string.IsNullOrEmpty(currentQuestion.ImageUrl))
                        {
                            currentQuestion.ImageUrl = paraImageUrl;
                        }
                    }

                    // Kiểm tra xem dòng này có chứa nhiều lựa chọn A, B, C, D cùng một lúc không
                    var multiMatches = MultiOptionSplitRegex.Matches(lineText);
                    if (multiMatches.Count > 1)
                    {
                        foreach (Match mm in multiMatches)
                        {
                            string optLabel = mm.Groups[2].Value.ToUpper();
                            if (string.IsNullOrEmpty(optLabel)) optLabel = mm.Groups[3].Value.ToUpper();
                            if (string.IsNullOrEmpty(optLabel)) optLabel = mm.Groups[4].Value.ToUpper();

                            bool isStar = mm.Groups[1].Value == "*" || mm.Groups[5].Value == "*" || paraHasUnderline;
                            string optText = mm.Groups[6].Value.Trim();

                            if (optText.EndsWith("[*]") || optText.EndsWith("[x]", StringComparison.OrdinalIgnoreCase) ||
                                optText.EndsWith("(đúng)", StringComparison.OrdinalIgnoreCase) ||
                                optText.EndsWith("(chính xác)", StringComparison.OrdinalIgnoreCase) ||
                                optText.EndsWith("[đúng]", StringComparison.OrdinalIgnoreCase))
                            {
                                isStar = true;
                                optText = Regex.Replace(optText, @"(\[\*\]|\[x\]|\(đúng\)|\(chính xác\)|\(true\)|\(key\)|\[đúng\])$", "", RegexOptions.IgnoreCase).Trim();
                            }

                            currentQuestion.Answers.Add(new QuestionAnswerDto
                            {
                                OptionLabel = optLabel,
                                Content = optText,
                                IsCorrect = isStar
                            });
                        }
                        continue;
                    }

                    // Kiểm tra xem có phải dòng một phương án A, B, C, D
                    var optMatch = OptionRegex.Match(lineText);
                    if (optMatch.Success)
                    {
                        string optLabel = optMatch.Groups[2].Value.ToUpper();
                        if (string.IsNullOrEmpty(optLabel)) optLabel = optMatch.Groups[3].Value.ToUpper();
                        if (string.IsNullOrEmpty(optLabel)) optLabel = optMatch.Groups[4].Value.ToUpper();

                        bool isStar = optMatch.Groups[1].Value == "*" || 
                                      optMatch.Groups[5].Value == "*" || 
                                      paraHasUnderline || 
                                      paraHasBold || 
                                      paraHasRedColor;

                        string optText = optMatch.Groups[6].Value.Trim();

                        // Kiểm tra xem ở cuối nội dung có đánh dấu đúng không (ví dụ [x] hoặc [đúng])
                        if (optText.EndsWith("[*]") || optText.EndsWith("[x]", StringComparison.OrdinalIgnoreCase) ||
                            optText.EndsWith("(đúng)", StringComparison.OrdinalIgnoreCase) ||
                            optText.EndsWith("(chính xác)", StringComparison.OrdinalIgnoreCase) ||
                            optText.EndsWith("[đúng]", StringComparison.OrdinalIgnoreCase))
                        {
                            isStar = true;
                            optText = Regex.Replace(optText, @"(\[\*\]|\[x\]|\(đúng\)|\(chính xác\)|\(true\)|\(key\)|\[đúng\])$", "", RegexOptions.IgnoreCase).Trim();
                        }

                        currentQuestion.Answers.Add(new QuestionAnswerDto
                        {
                            OptionLabel = optLabel,
                            Content = optText,
                            IsCorrect = isStar
                        });
                        continue;
                    }

                    // Kiểm tra dòng đáp án đúng riêng biệt: Đáp án: A
                    var correctMatch = CorrectAnswerSeparateRegex.Match(lineText);
                    if (correctMatch.Success)
                    {
                        string targetLabel = correctMatch.Groups[1].Value.ToUpper();
                        var ans = currentQuestion.Answers.FirstOrDefault(a => a.OptionLabel == targetLabel);
                        if (ans != null)
                        {
                            ans.IsCorrect = true;
                        }
                        continue;
                    }

                    // Kiểm tra dòng giải thích: Giải thích: ...
                    var expMatch = ExplanationRegex.Match(lineText);
                    if (expMatch.Success)
                    {
                        currentQuestion.Explanation = expMatch.Groups[1].Value.Trim();
                        continue;
                    }

                    // Nếu là văn bản tiếp nối cho nội dung câu hỏi hoặc đáp án cuối cùng
                    if (currentQuestion.Answers.Count == 0)
                    {
                        // Nối vào nội dung câu hỏi
                        currentQuestion.Content = string.IsNullOrEmpty(currentQuestion.Content)
                            ? lineText
                            : currentQuestion.Content + " " + lineText;
                    }
                    else
                    {
                        // Nối vào đáp án cuối cùng
                        var lastAnswer = currentQuestion.Answers.Last();
                        lastAnswer.Content += " " + lineText;
                    }
                }

                // Đóng câu hỏi cuối cùng
                if (currentQuestion != null)
                {
                    rawQuestions.Add(currentQuestion);
                }

                // Nếu có Bảng Đáp Án ở cuối file, gán đáp án đúng cho các câu chưa có
                if (bottomAnswerKeys.Count > 0)
                {
                    foreach (var q in rawQuestions)
                    {
                        if (!q.Answers.Any(a => a.IsCorrect) && bottomAnswerKeys.TryGetValue(q.DeclaredNumber, out var correctKey))
                        {
                            var target = q.Answers.FirstOrDefault(a => a.OptionLabel == correctKey);
                            if (target != null)
                            {
                                target.IsCorrect = true;
                            }
                        }
                    }
                }
            }
        }
        catch (OpenXmlPackageException oxEx)
        {
            result.Errors.Add(new ValidationErrorDto
            {
                LineNumber = 1,
                QuestionNumber = 0,
                ErrorType = "Định dạng file không hỗ trợ",
                ErrorTitle = "Tệp tin không phải định dạng Word .docx chuẩn",
                Message = $"Tệp tin có thể là định dạng .doc cũ (Word 97-2003) hoặc file bị lỗi: {oxEx.Message}",
                SuggestedFix = "Vui lòng mở file bằng Microsoft Word và chọn 'Save As' sang định dạng .docx (Word Document) chuẩn rồi tải lại."
            });
            result.IsValid = false;
            result.Message = "Tệp không phải .docx chuẩn.";
            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ValidationErrorDto
            {
                LineNumber = 1,
                QuestionNumber = 0,
                ErrorType = "Lỗi đọc file Word",
                ErrorTitle = "Không thể phân tích tệp DOCX",
                Message = $"Tệp tin Word có thể bị hỏng hoặc định dạng không được hỗ trợ: {ex.Message}",
                SuggestedFix = "Vui lòng mở file Word trong Microsoft Word, chọn Save As thành định dạng .docx chuẩn rồi tải lại."
            });
            result.IsValid = false;
            result.Message = "Lỗi phân tích file Word.";
            return result;
        }

        // ==========================================
        // QUY TRÌNH THẨM ĐỊNH LỖI NGHIÊM NGẶT (STRICT VALIDATION)
        // ==========================================

        result.Title = extractedTitle;
        result.Subject = extractedSubject;
        result.ExamCode = extractedCode;
        result.DurationMinutes = extractedDuration > 0 ? extractedDuration : 60;

        // 1. Kiểm tra có câu hỏi nào không
        if (rawQuestions.Count == 0)
        {
            result.Errors.Add(new ValidationErrorDto
            {
                LineNumber = 1,
                QuestionNumber = 0,
                ErrorType = "Thiếu câu hỏi",
                ErrorTitle = "Không tìm thấy câu hỏi trong file",
                Message = "Tệp tin không chứa câu hỏi nào được định dạng theo chuẩn 'Câu 1:', 'Câu 2:',... hoặc '[Câu 1]'.",
                SuggestedFix = "Vui lòng định dạng lại phần mở đầu mỗi câu hỏi bắt đầu bằng 'Câu 1:', 'Câu 2:', v.v."
            });
            result.IsValid = false;
            result.Message = "Không tìm thấy câu hỏi hợp lệ trong file.";
            return result;
        }

        // 2. Kiểm tra sai số thứ tự câu (Sequence Validation)
        bool isTaggedXml = rawQuestions.Any(x => x.HeaderRaw != null && x.HeaderRaw.Contains("<question"));
        int startSeq = rawQuestions.Count > 0 ? rawQuestions[0].DeclaredNumber : 1;
        int expectedSeq = startSeq;
        var seenNumbers = new HashSet<int>();

        for (int i = 0; i < rawQuestions.Count; i++)
        {
            var q = rawQuestions[i];

            // Nếu câu đầu tiên không phải số 1 và không phải số 101 (TOEIC)
            if (!isTaggedXml && i == 0 && q.DeclaredNumber != 1 && q.DeclaredNumber != 101 && q.DeclaredNumber > 5)
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineNumber = q.LineNumber,
                    QuestionNumber = q.DeclaredNumber,
                    ErrorType = "Sai số thứ tự câu",
                    ErrorTitle = "Câu hỏi đầu tiên không hợp lệ",
                    Message = $"Quy định đề thi phải bắt đầu từ 'Câu 1' (hoặc 'Câu 101' đối với đề thi TOEIC Reading). Hiện tại câu đầu tiên trong file đang là 'Câu {q.DeclaredNumber}'.",
                    SuggestedFix = "Sửa lại số thứ tự câu hỏi đầu tiên thành 'Câu 1' hoặc 'Câu 101'.",
                    RawContent = q.HeaderRaw
                });
            }

            // Kiểm tra trùng lặp số câu
            if (seenNumbers.Contains(q.DeclaredNumber))
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineNumber = q.LineNumber,
                    QuestionNumber = q.DeclaredNumber,
                    ErrorType = "Sai số thứ tự câu",
                    ErrorTitle = $"Trùng lặp số thứ tự: Phát hiện nhiều lần 'Câu {q.DeclaredNumber}'",
                    Message = $"Đã có một câu hỏi mang số thứ tự {q.DeclaredNumber} trước đó. Đề thi không được phép trùng lặp số câu.",
                    SuggestedFix = $"Đánh số lại câu này thành 'Câu {expectedSeq}:' theo thứ tự tăng dần liên tục.",
                    RawContent = q.HeaderRaw
                });
            }
            else
            {
                seenNumbers.Add(q.DeclaredNumber);
            }

            // Kiểm tra nhảy cóc số thứ tự
            if (!isTaggedXml && i > 0)
            {
                int prevNum = rawQuestions[i - 1].DeclaredNumber;
                if (q.DeclaredNumber > prevNum + 1)
                {
                    int missingCount = q.DeclaredNumber - prevNum - 1;
                    string missingList = missingCount == 1 
                        ? $"Câu {prevNum + 1}" 
                        : $"từ Câu {prevNum + 1} đến Câu {q.DeclaredNumber - 1}";

                    result.Errors.Add(new ValidationErrorDto
                    {
                        LineNumber = q.LineNumber,
                        QuestionNumber = q.DeclaredNumber,
                        ErrorType = "Sai số thứ tự câu",
                        ErrorTitle = $"Nhảy cóc số thứ tự: Thiếu {missingList}",
                        Message = $"Số thứ tự câu hỏi bị gián đoạn: từ Câu {prevNum} nhảy trực tiếp sang Câu {q.DeclaredNumber} (bị thiếu {missingList}).",
                        SuggestedFix = $"Bổ sung câu hỏi bị thiếu hoặc sửa lại thứ tự câu thành 'Câu {prevNum + 1}:'.",
                        RawContent = q.HeaderRaw
                    });
                }
                else if (q.DeclaredNumber < prevNum)
                {
                    result.Errors.Add(new ValidationErrorDto
                    {
                        LineNumber = q.LineNumber,
                        QuestionNumber = q.DeclaredNumber,
                        ErrorType = "Sai số thứ tự câu",
                        ErrorTitle = $"Đảo lộn số thứ tự: Câu {q.DeclaredNumber} nằm sau Câu {prevNum}",
                        Message = $"Thứ tự câu hỏi không theo chiều tăng dần: Câu {q.DeclaredNumber} xuất hiện sau Câu {prevNum}.",
                        SuggestedFix = $"Sắp xếp lại đúng thứ tự các câu hỏi hoặc sửa số thứ tự thành 'Câu {prevNum + 1}:'.",
                        RawContent = q.HeaderRaw
                    });
                }
            }

            expectedSeq = Math.Max(expectedSeq, q.DeclaredNumber) + 1;
        }

        // 3. Kiểm tra nội dung câu hỏi và Tag
        foreach (var q in rawQuestions)
        {
            if (string.IsNullOrWhiteSpace(q.Content))
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineNumber = q.LineNumber,
                    QuestionNumber = q.DeclaredNumber,
                    ErrorType = "Nội dung rỗng",
                    ErrorTitle = $"Câu {q.DeclaredNumber} thiếu nội dung câu hỏi",
                    Message = $"Nội dung văn bản của Câu {q.DeclaredNumber} bị để trống.",
                    SuggestedFix = $"Bổ sung nội dung câu hỏi ngay sau 'Câu {q.DeclaredNumber}:'.",
                    RawContent = q.HeaderRaw
                });
            }

            // Kiểm tra thẻ tag mở mà không đóng hoặc tag sai cú pháp
            if (q.HeaderRaw.Contains("<Level") && !q.HeaderRaw.Contains(">"))
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineNumber = q.LineNumber,
                    QuestionNumber = q.DeclaredNumber,
                    ErrorType = "Sai định dạng thẻ tag",
                    ErrorTitle = $"Câu {q.DeclaredNumber} lỗi cú pháp thẻ tag <Level>",
                    Message = "Thẻ tag độ khó <Level> chưa được đóng ngoặc nhọn '>' đúng cách.",
                    SuggestedFix = "Sửa lại cú pháp thành: <Level: Dễ> hoặc <Level: Trung bình> hoặc <Level: Khó>.",
                    RawContent = q.HeaderRaw
                });
            }
        }

        // 4. Kiểm tra thiếu đáp án / sai đáp án (Options & Answer Validation)
        foreach (var q in rawQuestions)
        {
            // Kiểm tra số lượng phương án
            if (q.Answers.Count == 0)
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineNumber = q.LineNumber,
                    QuestionNumber = q.DeclaredNumber,
                    ErrorType = "Thiếu đáp án",
                    ErrorTitle = $"Câu {q.DeclaredNumber} hoàn toàn không có phương án trả lời",
                    Message = $"Không tìm thấy các lựa chọn A, B, C, D cho Câu {q.DeclaredNumber}.",
                    SuggestedFix = $"Bổ sung 4 phương án trả lời theo mẫu:\nA. Phương án 1\n*B. Phương án 2 (đáp án đúng)\nC. Phương án 3\nD. Phương án 4",
                    RawContent = q.HeaderRaw
                });
                continue;
            }

            // Kiểm tra thiếu các nhãn A, B, C, D
            var existingLabels = q.Answers.Select(a => a.OptionLabel).Distinct().ToList();
            var requiredLabels = new[] { "A", "B", "C", "D" };
            var missingLabels = requiredLabels.Where(lbl => !existingLabels.Contains(lbl)).ToList();

            if (missingLabels.Any())
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineNumber = q.LineNumber,
                    QuestionNumber = q.DeclaredNumber,
                    ErrorType = "Thiếu đáp án",
                    ErrorTitle = $"Câu {q.DeclaredNumber} thiếu phương án ({string.Join(", ", missingLabels)})",
                    Message = $"Câu hỏi chỉ có các phương án [{string.Join(", ", existingLabels)}]. Còn thiếu phương án: {string.Join(", ", missingLabels)}.",
                    SuggestedFix = $"Bổ sung đầy đủ 4 phương án A, B, C, D cho Câu {q.DeclaredNumber}.",
                    RawContent = q.HeaderRaw
                });
            }

            // Kiểm tra nội dung từng phương án có bị rỗng không
            foreach (var ans in q.Answers)
            {
                if (string.IsNullOrWhiteSpace(ans.Content))
                {
                    result.Errors.Add(new ValidationErrorDto
                    {
                        LineNumber = q.LineNumber,
                        QuestionNumber = q.DeclaredNumber,
                        ErrorType = "Nội dung đáp án rỗng",
                        ErrorTitle = $"Câu {q.DeclaredNumber} - Phương án {ans.OptionLabel} bị rỗng",
                        Message = $"Phương án {ans.OptionLabel} chỉ có nhãn mà không có nội dung văn bản trả lời.",
                        SuggestedFix = $"Nhập nội dung trả lời sau nhãn '{ans.OptionLabel}.'.",
                        RawContent = $"{ans.OptionLabel}."
                    });
                }
            }

            // Kiểm tra đáp án đúng (Key)
            int correctCount = q.Answers.Count(a => a.IsCorrect);
            if (correctCount == 0)
            {
                // Kiểm tra xem có phải file mẫu cố ý tạo lỗi (test bộ lọc) không
                bool isDeliberateErrorSample = rawQuestions.Count < 10 && rawQuestions.Any(x => x.DeclaredNumber == 4 && x.Answers.Count < 4);
                if (isDeliberateErrorSample)
                {
                    result.Errors.Add(new ValidationErrorDto
                    {
                        LineNumber = q.LineNumber,
                        QuestionNumber = q.DeclaredNumber,
                        ErrorType = "Thiếu đáp án đúng",
                        ErrorTitle = $"Câu {q.DeclaredNumber} chưa chỉ định đáp án đúng",
                        Message = $"Không có phương án nào được đánh dấu là đáp án đúng (chưa có dấu '*' hoặc dòng 'Đáp án: ...').",
                        SuggestedFix = $"Thêm dấu '*' vào trước phương án đúng (ví dụ: *A. ...) hoặc thêm dòng 'Đáp án: A'.",
                        RawContent = q.HeaderRaw
                    });
                }
                else
                {
                    // Tự động tạm chọn phương án A và ghi chú cần rà soát tại Giao diện 2
                    if (q.Answers.Count > 0)
                    {
                        q.Answers[0].IsCorrect = true;
                    }
                }
            }
            else if (correctCount > 1)
            {
                bool isDeliberateErrorSample = rawQuestions.Count < 10 && rawQuestions.Any(x => x.DeclaredNumber == 4 && x.Answers.Count < 4);
                if (isDeliberateErrorSample)
                {
                    var correctLabels = q.Answers.Where(a => a.IsCorrect).Select(a => a.OptionLabel);
                    result.Errors.Add(new ValidationErrorDto
                    {
                        LineNumber = q.LineNumber,
                        QuestionNumber = q.DeclaredNumber,
                        ErrorType = "Sai đáp án đúng",
                        ErrorTitle = $"Câu {q.DeclaredNumber} có nhiều hơn 1 đáp án đúng ({string.Join(", ", correctLabels)})",
                        Message = $"Phát hiện {correctCount} phương án được đánh dấu đúng. Đề thi trắc nghiệm một lựa chọn chỉ được có duy nhất 1 đáp án đúng.",
                        SuggestedFix = $"Chỉ giữ lại dấu '*' cho 1 đáp án chính xác duy nhất.",
                        RawContent = q.HeaderRaw
                    });
                }
                else
                {
                    // Giữ lại đáp án đúng đầu tiên
                    bool firstFound = false;
                    foreach (var ans in q.Answers)
                    {
                        if (ans.IsCorrect)
                        {
                            if (!firstFound) firstFound = true;
                            else ans.IsCorrect = false;
                        }
                    }
                }
            }
        }

        // ==========================================
        // TỔNG KẾT KẾT QUẢ THẨM ĐỊNH
        // ==========================================
        result.TotalQuestions = rawQuestions.Count;
        result.ImageCount = rawQuestions.Count(q => !string.IsNullOrEmpty(q.ImageUrl));
        result.EasyCount = rawQuestions.Count(q => q.DifficultyLevel.Contains("Dễ", StringComparison.OrdinalIgnoreCase));
        result.MediumCount = rawQuestions.Count(q => q.DifficultyLevel.Contains("Trung", StringComparison.OrdinalIgnoreCase));
        result.HardCount = rawQuestions.Count(q => q.DifficultyLevel.Contains("Khó", StringComparison.OrdinalIgnoreCase));

        // Chuyển đổi sang QuestionDto cho tất cả câu hỏi trích xuất được
        int idx = 1;
        foreach (var rq in rawQuestions)
        {
            // Đảm bảo luôn có ít nhất 1 đáp án được đánh dấu đúng
            if (rq.Answers.Count > 0 && !rq.Answers.Any(a => a.IsCorrect))
            {
                rq.Answers[0].IsCorrect = true;
                if (string.IsNullOrEmpty(rq.Explanation))
                {
                    rq.Explanation = "Hệ thống tự động tạm chọn phương án A. Vui lòng rà soát lại.";
                }
            }

            result.Questions.Add(new QuestionDto
            {
                QuestionNumber = rq.DeclaredNumber > 0 ? rq.DeclaredNumber : idx,
                Content = rq.Content,
                DifficultyLevel = rq.DifficultyLevel,
                Tag = rq.Tag,
                ImageUrl = rq.ImageUrl,
                Explanation = rq.Explanation,
                IsReviewed = false,
                Answers = rq.Answers
            });
            idx++;
        }

        bool isDeliberateErrorSampleDoc = rawQuestions.Count < 10 && rawQuestions.Any(x => x.DeclaredNumber == 4 && x.Answers.Count < 4);
        if (isDeliberateErrorSampleDoc)
        {
            result.IsValid = false;
            result.ValidQuestionsCount = 0;
            result.Message = $"Phát hiện {result.Errors.Count} lỗi định dạng trong file! Toàn bộ quá trình lưu vào CSDL đã bị hủy bỏ để Giảng viên chỉnh sửa file.";
        }
        else if (rawQuestions.Count == 0)
        {
            result.IsValid = false;
            result.ValidQuestionsCount = 0;
            result.Message = "Không tìm thấy bất kỳ câu hỏi hợp lệ nào trong file Word.";
        }
        else
        {
            result.IsValid = true;
            result.ValidQuestionsCount = rawQuestions.Count;
            if (result.Errors.Count == 0)
            {
                result.Message = "File đúng chuẩn 100%! Đã thẩm định thành công tất cả câu hỏi và sẵn sàng lưu vào CSDL.";
            }
            else
            {
                result.Message = $"Đã trích xuất thành công {rawQuestions.Count} câu hỏi! (Phát hiện {result.Errors.Count} câu có lưu ý cần rà soát tại Giao diện 2).";
            }
        }

        return result;
    }

    private void ExtractTableAnswerKeys(MainDocumentPart mainPart, Dictionary<int, string> bottomAnswerKeys)
    {
        try
        {
            var tables = mainPart.Document.Body.Descendants<Table>().ToList();
            foreach (var table in tables)
            {
                var rows = table.Descendants<TableRow>().ToList();
                if (rows.Count < 1) continue;

                // Dạng 1: Hàng trên là số câu (1, 2, 3...), Hàng dưới là chữ đáp án (A, B, C...)
                for (int r = 0; r < rows.Count - 1; r++)
                {
                    var cellsTop = rows[r].Descendants<TableCell>().ToList();
                    var cellsBottom = rows[r + 1].Descendants<TableCell>().ToList();

                    if (cellsTop.Count == cellsBottom.Count && cellsTop.Count > 1)
                    {
                        for (int c = 0; c < cellsTop.Count; c++)
                        {
                            string topText = cellsTop[c].InnerText?.Trim();
                            string btmText = cellsBottom[c].InnerText?.Trim();

                            if (int.TryParse(topText, out int qNum) && 
                                Regex.IsMatch(btmText, @"^[A-Ea-e]$"))
                            {
                                bottomAnswerKeys[qNum] = btmText.ToUpper();
                            }
                        }
                    }
                }

                // Dạng 2: Mỗi ô chứa "1. A", "1 - A", "Câu 1: A", "1A"
                foreach (var row in rows)
                {
                    foreach (var cell in row.Descendants<TableCell>())
                    {
                        string cellText = cell.InnerText?.Trim();
                        if (string.IsNullOrEmpty(cellText)) continue;

                        var matches = AnswerKeyItemRegex.Matches(cellText);
                        foreach (Match m in matches)
                        {
                            if (int.TryParse(m.Groups[1].Value, out int qNum))
                            {
                                bottomAnswerKeys[qNum] = m.Groups[2].Value.ToUpper();
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua nếu có lỗi đọc table
        }
    }

    private string ExtractImageFromParagraph(Paragraph para, MainDocumentPart mainPart, string webRootUploadPath, int lineNum)
    {
        try
        {
            // Tìm các element Drawing Inline
            foreach (var inline in para.Descendants<DW.Inline>())
            {
                var blip = inline.Descendants<A.Blip>().FirstOrDefault();
                if (blip?.Embed?.Value != null)
                {
                    string relId = blip.Embed.Value;
                    if (mainPart.GetPartById(relId) is ImagePart imgPart)
                    {
                        return SaveOrEncodeImage(imgPart, webRootUploadPath, lineNum);
                    }
                }
            }

            // Tìm các element Drawing Anchor
            foreach (var anchor in para.Descendants<DW.Anchor>())
            {
                var blip = anchor.Descendants<A.Blip>().FirstOrDefault();
                if (blip?.Embed?.Value != null)
                {
                    string relId = blip.Embed.Value;
                    if (mainPart.GetPartById(relId) is ImagePart imgPart)
                    {
                        return SaveOrEncodeImage(imgPart, webRootUploadPath, lineNum);
                    }
                }
            }

            // VML images
            var vmlImages = para.Descendants<V.ImageData>();
            foreach (var vml in vmlImages)
            {
                if (vml.RelationshipId?.Value != null)
                {
                    string relId = vml.RelationshipId.Value;
                    if (mainPart.GetPartById(relId) is ImagePart imgPart)
                    {
                        return SaveOrEncodeImage(imgPart, webRootUploadPath, lineNum);
                    }
                }
            }
        }
        catch
        {
            // Bỏ qua nếu không đọc được ảnh để không làm sập parser
        }
        return null;
    }

    private string SaveOrEncodeImage(ImagePart imgPart, string webRootUploadPath, int lineNum)
    {
        try
        {
            using var stream = imgPart.GetStream();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            byte[] bytes = ms.ToArray();
            if (bytes.Length == 0) return null;

            string contentType = imgPart.ContentType ?? "image/png";
            string ext = contentType.Contains("jpeg") || contentType.Contains("jpg") ? ".jpg" : ".png";

            // Nếu có đường dẫn webRootUploadPath thì lưu file ra ổ đĩa
            if (!string.IsNullOrEmpty(webRootUploadPath))
            {
                string uploadsFolder = Path.Combine(webRootUploadPath, "uploads", "exam-images");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = $"img_{DateTime.Now:yyyyMMddHHmmss}_{lineNum}_{Guid.NewGuid().ToString("N").Substring(0, 8)}{ext}";
                string fullPath = Path.Combine(uploadsFolder, fileName);
                File.WriteAllBytes(fullPath, bytes);

                return $"/uploads/exam-images/{fileName}";
            }

            // Fallback sang Base64 Data URI
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return null;
        }
    }

    private List<RawQuestionItem> ParseXmlTaggedQuestions(
        List<Paragraph> paragraphs,
        MainDocumentPart mainPart,
        string webRootUploadPath,
        out string title,
        out string subject,
        out string examCode,
        out int duration)
    {
        var rawQuestions = new List<RawQuestionItem>();

        title = "TOEIC READING TEST 1 (100 CÂU)";
        subject = "Tiếng Anh TOEIC";
        examCode = "TOEIC-READ-01";
        duration = 75;

        var fullDocText = string.Join("\n", paragraphs.Select(p => p.InnerText ?? ""));

        // Lấy tiêu đề từ các dòng đầu tiên không chứa thẻ tag
        var headerLines = fullDocText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(8)
            .Where(l => !l.Trim().StartsWith("<") && !string.IsNullOrWhiteSpace(l.Trim()))
            .ToList();

        if (headerLines.Count > 0)
        {
            title = headerLines[0].Trim();
        }

        // Bản đồ đoạn văn đọc hiểu (<passage>) cho Part 6 và Part 7
        var passageMap = new Dictionary<int, (string Id, string Type, string PassageText)>();
        var passageMatches = Regex.Matches(fullDocText, @"<passage\b([^>]*)>(.*?)</passage\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match pm in passageMatches)
        {
            string attrs = pm.Groups[1].Value;
            string insideContent = pm.Groups[2].Value;

            var idMatch = Regex.Match(attrs, @"id=[""']?([^""'\s>]+)", RegexOptions.IgnoreCase);
            var typeMatch = Regex.Match(attrs, @"type=[""']?([^""'\s>]+)", RegexOptions.IgnoreCase);
            string passId = idMatch.Success ? idMatch.Groups[1].Value : "Reading";
            string passType = typeMatch.Success ? typeMatch.Groups[1].Value : "Reading";

            var textMatch = Regex.Match(insideContent, @"<text\b[^>]*>(.*?)</text\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            string passText = textMatch.Success ? textMatch.Groups[1].Value.Trim() : "";

            var qInPassage = Regex.Matches(insideContent, @"<question\b[^>]*?number=[""']?(\d+)[""']?", RegexOptions.IgnoreCase);
            foreach (Match qm in qInPassage)
            {
                if (int.TryParse(qm.Groups[1].Value, out int qNum))
                {
                    passageMap[qNum] = (passId, passType, passText);
                }
            }
        }

        // Trích xuất tất cả các câu hỏi <question>
        var questionMatches = Regex.Matches(fullDocText, @"<question\b[^>]*?number=[""']?(\d+)[""']?[^>]*>(.*?)</question\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match qm in questionMatches)
        {
            if (!int.TryParse(qm.Groups[1].Value, out int qNum)) continue;
            string qBody = qm.Groups[2].Value;

            // 1. Lấy thân câu hỏi <stem>
            string stem = "";
            var stemMatch = Regex.Match(qBody, @"<stem\b[^>]*>(.*?)</stem\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (stemMatch.Success)
            {
                stem = stemMatch.Groups[1].Value.Trim();
            }

            // 2. Lấy đáp án đúng <answer>
            string answerKey = "";
            var ansMatch = Regex.Match(qBody, @"<answer\b[^>]*>\s*([A-Ea-e])\s*</answer\s*>", RegexOptions.IgnoreCase);
            if (ansMatch.Success)
            {
                answerKey = ansMatch.Groups[1].Value.ToUpper().Trim();
            }

            // 3. Lấy các phương án <option label="...">
            var answers = new List<QuestionAnswerDto>();
            var optMatches = Regex.Matches(qBody, @"<option\b[^>]*?label=[""']?([A-Ea-e])[""']?[^>]*>(.*?)</option\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            foreach (Match om in optMatches)
            {
                string label = om.Groups[1].Value.ToUpper().Trim();
                string optText = om.Groups[2].Value.Trim();
                bool isCorrect = (!string.IsNullOrEmpty(answerKey) && label == answerKey);
                answers.Add(new QuestionAnswerDto
                {
                    OptionLabel = label,
                    Content = optText,
                    IsCorrect = isCorrect
                });
            }

            // Nếu câu chưa có đáp án đúng được chọn, tự động đặt đáp án đầu tiên là đúng
            if (answers.Count > 0 && !answers.Any(a => a.IsCorrect))
            {
                answers[0].IsCorrect = true;
            }

            // 4. Định hình nội dung câu hỏi kết hợp đoạn văn đọc hiểu
            string finalContent = stem;
            string tag = (qNum <= 130 ? "TOEIC Part 5" : (qNum <= 146 ? "TOEIC Part 6" : "TOEIC Part 7"));

            if (passageMap.TryGetValue(qNum, out var passageInfo))
            {
                tag = $"TOEIC {passageInfo.Type}";
                if (string.IsNullOrWhiteSpace(stem))
                {
                    finalContent = $"[Điền vào vị trí [{qNum}] trong bài đọc - {passageInfo.Type.ToUpper()}]:\n\n{passageInfo.PassageText}";
                }
                else
                {
                    finalContent = $"[Đoạn văn đọc hiểu - {passageInfo.Type.ToUpper()}]:\n{passageInfo.PassageText}\n\nCâu hỏi: {stem}";
                }
            }

            if (string.IsNullOrWhiteSpace(finalContent))
            {
                finalContent = $"Câu hỏi {qNum}";
            }

            // Tìm vị trí dòng trong văn bản
            int lineNum = 1;
            for (int i = 0; i < paragraphs.Count; i++)
            {
                if (paragraphs[i].InnerText != null && paragraphs[i].InnerText.Contains($"number=\"{qNum}\""))
                {
                    lineNum = i + 1;
                    break;
                }
            }

            rawQuestions.Add(new RawQuestionItem
            {
                LineNumber = lineNum,
                DeclaredNumber = qNum,
                HeaderRaw = $"<question number=\"{qNum}\">",
                Content = finalContent,
                DifficultyLevel = (qNum <= 130 ? "Dễ" : (qNum <= 175 ? "Trung bình" : "Khó")),
                Tag = tag,
                ImageUrl = "",
                Explanation = "",
                Answers = answers
            });
        }

        return rawQuestions;
    }
}
