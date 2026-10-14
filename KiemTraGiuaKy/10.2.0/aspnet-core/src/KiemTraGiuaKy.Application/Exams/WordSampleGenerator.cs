using System;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace KiemTraGiuaKy.Exams;

public static class WordSampleGenerator
{
    public static byte[] GenerateStandardSampleDocx()
    {
        using var mem = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Header metadata
            AddHeading(body, "KỲ THI ĐÁNH GIÁ NĂNG LỰC GIỮA KỲ", 28, true);
            AddParagraph(body, "[Tên đề]: Đề kiểm tra giữa kỳ - Lập trình trên môi trường di động");
            AddParagraph(body, "[Môn học]: Lập trình trên môi trường di động");
            AddParagraph(body, "[Mã đề]: MOB-2026-01");
            AddParagraph(body, "[Thời gian]: 60 phút");
            AddParagraph(body, "--------------------------------------------------");

            // Câu 1
            AddParagraph(body, "Câu 1: Vòng đời của một Activity trong Android bắt đầu bằng phương thức nào? <Level: Dễ> <Tag: Kiến trúc Android>");
            AddParagraph(body, "*A. onCreate()");
            AddParagraph(body, "B. onStart()");
            AddParagraph(body, "C. onResume()");
            AddParagraph(body, "D. onRestart()");
            AddParagraph(body, "[Giải thích]: onCreate() là hàm callback đầu tiên được hệ điều hành Android gọi khi tạo một Activity mới.");
            AddParagraph(body, "");

            // Câu 2 (Có kèm ảnh minh họa)
            AddParagraph(body, "Câu 2: Quan sát sơ đồ vòng đời Activity trong hình ảnh dưới đây. Trạng thái nào xảy ra khi Activity bị che khuất một phần bởi một Dialog? <Level: Trung bình> <Tag: Android Lifecycle>");
            
            // Chèn ảnh minh họa vào Word
            InsertSampleImage(mainPart, body);

            AddParagraph(body, "A. onStop()");
            AddParagraph(body, "*B. onPause()");
            AddParagraph(body, "C. onDestroy()");
            AddParagraph(body, "D. onRestart()");
            AddParagraph(body, "[Giải thích]: onPause() được gọi khi Activity mất tiêu điểm nhưng vẫn còn nhìn thấy một phần trên màn hình.");
            AddParagraph(body, "");

            // Câu 3
            AddParagraph(body, "Câu 3: Trong ngôn ngữ lập trình Kotlin, từ khóa nào được dùng để khai báo một biến chỉ đọc (read-only / bất biến)? <Level: Dễ> <Tag: Ngôn ngữ Kotlin>");
            AddParagraph(body, "A. var");
            AddParagraph(body, "*B. val");
            AddParagraph(body, "C. const");
            AddParagraph(body, "D. let");
            AddParagraph(body, "[Giải thích]: Từ khóa 'val' trong Kotlin tương đương với 'final' trong Java, giá trị không thể thay đổi sau khi gán.");
            AddParagraph(body, "");

            // Câu 4
            AddParagraph(body, "Câu 4: Trong Flutter, thành phần nào quản lý cây widget động và có thể vẽ lại giao diện khi dữ liệu thay đổi thông qua hàm setState()? <Level: Khó> <Tag: Flutter / Dart>");
            AddParagraph(body, "A. StatelessWidget");
            AddParagraph(body, "*B. StatefulWidget");
            AddParagraph(body, "C. InheritedWidget");
            AddParagraph(body, "D. RenderObjectWidget");
            AddParagraph(body, "[Giải thích]: StatefulWidget cho phép quản lý trạng thái có thể biến đổi theo thời gian bằng State object.");
            AddParagraph(body, "");

            // Câu 5
            AddParagraph(body, "Câu 5: Trong Jetpack Compose, hàm nào được sử dụng để xếp chồng các phần tử giao diện theo chiều dọc từ trên xuống dưới? <Level: Dễ> <Tag: Jetpack Compose>");
            AddParagraph(body, "A. Row");
            AddParagraph(body, "*B. Column");
            AddParagraph(body, "C. Box");
            AddParagraph(body, "D. Spacer");
            AddParagraph(body, "[Giải thích]: Column là composable layout sắp xếp các phần tử con theo trục thẳng đứng.");

            wordDoc.Save();
        }
        return mem.ToArray();
    }

    public static byte[] GenerateErrorSampleDocx()
    {
        using var mem = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Header metadata
            AddHeading(body, "ĐỀ THI MẪU CỐ Ý CÓ LỖI (ĐỂ TEST BỘ LỌC ĐỊNH DẠNG)", 24, true);
            AddParagraph(body, "[Tên đề]: Đề thi thử nghiệm kiểm tra lỗi định dạng");
            AddParagraph(body, "[Môn học]: Lập trình trên môi trường di động");
            AddParagraph(body, "--------------------------------------------------");

            // Câu 1: Bình thường
            AddParagraph(body, "Câu 1: Ngôn ngữ chính thức do Google khuyến nghị để phát triển ứng dụng Android native là gì? <Level: Dễ> <Tag: Kotlin>");
            AddParagraph(body, "*A. Kotlin");
            AddParagraph(body, "B. Swift");
            AddParagraph(body, "C. C#");
            AddParagraph(body, "D. Python");
            AddParagraph(body, "");

            // LỖI 1: Nhảy cóc số thứ tự từ Câu 1 sang Câu 3 (Thiếu Câu 2)
            AddParagraph(body, "Câu 3: File AndroidManifest.xml dùng để làm gì trong dự án Android? <Level: Trung bình> <Tag: Android Basics>");
            AddParagraph(body, "A. Định nghĩa giao diện người dùng");
            AddParagraph(body, "*B. Khai báo quyền, thành phần cấu hình cơ bản của ứng dụng");
            AddParagraph(body, "C. Viết mã nguồn xử lý logic");
            AddParagraph(body, "D. Quản lý thư viện phụ thuộc");
            AddParagraph(body, "");

            // LỖI 2: Thiếu đáp án (chỉ có A, B) VÀ không có đáp án đúng nào được chọn
            AddParagraph(body, "Câu 4: Gradle trong Android Studio có vai trò gì? <Level: Dễ> <Tag: Build Tool>");
            AddParagraph(body, "A. Trình biên dịch mã máy");
            AddParagraph(body, "B. Hệ thống tự động hóa build và quản lý thư viện");
            AddParagraph(body, "");

            // LỖI 3: Trùng lặp số thứ tự (Lại là Câu 4) VÀ sai cú pháp thẻ tag <Level (thiếu dấu đóng)
            AddParagraph(body, "Câu 4: Intent trong Android có tác dụng gì? <Level: Khó <Tag: Components>");
            AddParagraph(body, "*A. Dùng để truyền thông điệp giữa các Activity hoặc Service");
            AddParagraph(body, "*B. Khởi chạy một Activity mới (Lỗi: 2 đáp án đúng)");
            AddParagraph(body, "C. Lưu trữ dữ liệu lâu dài vào ổ đĩa");
            AddParagraph(body, "D. Vẽ đồ họa 3D");

            wordDoc.Save();
        }
        return mem.ToArray();
    }

    private static void AddHeading(Body body, string text, int fontSize, bool isBold)
    {
        var p = body.AppendChild(new Paragraph());
        var r = p.AppendChild(new Run());
        var rPr = r.AppendChild(new RunProperties());
        rPr.AppendChild(new FontSize { Val = (fontSize * 2).ToString() });
        if (isBold) rPr.AppendChild(new Bold());
        r.AppendChild(new Text(text));
    }

    private static void AddParagraph(Body body, string text)
    {
        var p = body.AppendChild(new Paragraph());
        var r = p.AppendChild(new Run());
        r.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }

    private static void InsertSampleImage(MainDocumentPart mainPart, Body body)
    {
        try
        {
            // Tạo 1 hình ảnh PNG nhỏ (1x1 hoặc ảnh SVG vẽ sơ đồ đơn giản dạng base64)
            // Chuỗi base64 của một hình ảnh PNG hợp lệ (sơ đồ đồ họa 400x160)
            string pngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAZAAAACgCAYAAADg3sU9AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAEKSURBVHhe7cExAQAAAMKg9U9tCF8gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADwZ+wABvR5m/AAAAABJRU5ErkJggg==";
            byte[] imageBytes = Convert.FromBase64String(pngBase64);

            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            using (var stream = new MemoryStream(imageBytes))
            {
                imagePart.FeedData(stream);
            }

            string relationshipId = mainPart.GetIdOfPart(imagePart);

            long widthEmus = 400 * 9525;
            long heightEmus = 160 * 9525;

            var pic = new PIC.Picture(
                new PIC.NonVisualPictureProperties(
                    new PIC.NonVisualDrawingProperties { Id = (UInt32Value)0U, Name = "SoDoVongDoi.png" },
                    new PIC.NonVisualPictureDrawingProperties()),
                new PIC.BlipFill(
                    new A.Blip { Embed = relationshipId },
                    new A.Stretch(new A.FillRectangle())),
                new PIC.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset { X = 0L, Y = 0L },
                        new A.Extents { Cx = widthEmus, Cy = heightEmus }),
                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }));

            var graphicData = new A.GraphicData(pic) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" };
            var graphic = new A.Graphic(graphicData);
            var inline = new DW.Inline(
                new DW.Extent { Cx = widthEmus, Cy = heightEmus },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DW.DocProperties { Id = (UInt32Value)1U, Name = "SoDoVongDoi" },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                graphic);

            var element = new Drawing(inline);
            var p = body.AppendChild(new Paragraph());
            var r = p.AppendChild(new Run(element));
        }
        catch
        {
            // Bỏ qua nếu lỗi chèn ảnh
        }
    }

    public static byte[] GenerateToeicSampleDocx()
    {
        using var mem = new MemoryStream();
        using (var wordDoc = WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            AddHeading(body, "TOEIC READING TEST 1 — PART 5 (QUESTIONS 101–130)", 26, true);
            AddParagraph(body, "<exam>");
            AddParagraph(body, "<part id=\"5\" type=\"independent\">");
            AddParagraph(body, "");

            // Câu 101
            AddParagraph(body, "<question number=\"101\">");
            AddParagraph(body, "<stem>All employees must submit their expense reports _____ Friday afternoon.</stem>");
            AddParagraph(body, "<options>");
            AddParagraph(body, "<option label=\"A\">at</option>");
            AddParagraph(body, "<option label=\"B\">by</option>");
            AddParagraph(body, "<option label=\"C\">on</option>");
            AddParagraph(body, "<option label=\"D\">until</option>");
            AddParagraph(body, "</options>");
            AddParagraph(body, "<answer>B</answer>");
            AddParagraph(body, "</question>");
            AddParagraph(body, "");

            // Câu 102
            AddParagraph(body, "<question number=\"102\">");
            AddParagraph(body, "<stem>The manager _____ the proposal before the meeting began.</stem>");
            AddParagraph(body, "<options>");
            AddParagraph(body, "<option label=\"A\">review</option>");
            AddParagraph(body, "<option label=\"B\">reviewing</option>");
            AddParagraph(body, "<option label=\"C\">had reviewed</option>");
            AddParagraph(body, "<option label=\"D\">has review</option>");
            AddParagraph(body, "</options>");
            AddParagraph(body, "<answer>C</answer>");
            AddParagraph(body, "</question>");
            AddParagraph(body, "");

            // Câu 103
            AddParagraph(body, "<question number=\"103\">");
            AddParagraph(body, "<stem>Please contact the customer service department if you have any _____ about your order.</stem>");
            AddParagraph(body, "<options>");
            AddParagraph(body, "<option label=\"A\">questions</option>");
            AddParagraph(body, "<option label=\"B\">questionable</option>");
            AddParagraph(body, "<option label=\"C\">questioning</option>");
            AddParagraph(body, "<option label=\"D\">questioned</option>");
            AddParagraph(body, "</options>");
            AddParagraph(body, "<answer>A</answer>");
            AddParagraph(body, "</question>");
            AddParagraph(body, "");

            // Câu 104
            AddParagraph(body, "<question number=\"104\">");
            AddParagraph(body, "<stem>The conference will take place in the main auditorium _____ 9:00 AM.</stem>");
            AddParagraph(body, "<options>");
            AddParagraph(body, "<option label=\"A\">in</option>");
            AddParagraph(body, "<option label=\"B\">at</option>");
            AddParagraph(body, "<option label=\"C\">on</option>");
            AddParagraph(body, "<option label=\"D\">for</option>");
            AddParagraph(body, "</options>");
            AddParagraph(body, "<answer>B</answer>");
            AddParagraph(body, "</question>");
            AddParagraph(body, "");

            // Câu 105
            AddParagraph(body, "<question number=\"105\">");
            AddParagraph(body, "<stem>Ms. Carter received an award for her outstanding _____ to the team.</stem>");
            AddParagraph(body, "<options>");
            AddParagraph(body, "<option label=\"A\">contribute</option>");
            AddParagraph(body, "<option label=\"B\">contribution</option>");
            AddParagraph(body, "<option label=\"C\">contributor</option>");
            AddParagraph(body, "<option label=\"D\">contributed</option>");
            AddParagraph(body, "</options>");
            AddParagraph(body, "<answer>B</answer>");
            AddParagraph(body, "</question>");
            AddParagraph(body, "");

            AddParagraph(body, "</part>");
            AddParagraph(body, "</exam>");

            wordDoc.Save();
        }
        return mem.ToArray();
    }
}
