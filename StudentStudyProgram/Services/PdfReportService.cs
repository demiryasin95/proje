using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;

namespace StudentStudyProgram.Services
{
    public class PdfReportService
    {
        private readonly string _fontPath;
        
        // Lavender color scheme
        private readonly BaseColor PrimaryColor = new BaseColor(148, 133, 228); // #9485E4
        private readonly BaseColor SecondaryColor = new BaseColor(123, 107, 199); // #7B6BC7
        private readonly BaseColor LightColor = new BaseColor(243, 241, 255); // #F3F1FF
        private readonly BaseColor VeryLightColor = new BaseColor(250, 249, 255); // #FAF9FF
        private readonly BaseColor GoldColor = new BaseColor(255, 215, 0); // #FFD700
        private readonly BaseColor SilverColor = new BaseColor(192, 192, 192); // #C0C0C0
        private readonly BaseColor BronzeColor = new BaseColor(205, 127, 50); // #CD7F32
        private readonly BaseColor SuccessColor = new BaseColor(16, 185, 129); // #10B981
        private readonly BaseColor WarningColor = new BaseColor(245, 158, 11); // #F59E0B
        private readonly BaseColor DangerColor = new BaseColor(239, 68, 68); // #EF4444

        public PdfReportService()
        {
            // Try multiple font paths for cross-platform compatibility
            var possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Arial.ttf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
                @"C:\Windows\Fonts\Arial.ttf",
                @"C:\Windows\Fonts\arial.ttf"
            };

            _fontPath = possiblePaths.FirstOrDefault(File.Exists);
            
            // Fallback to embedded Helvetica if Arial is not found
            if (string.IsNullOrEmpty(_fontPath))
            {
                _fontPath = null; // Will use BaseFont.HELVETICA as fallback
            }
        }

        public byte[] GenerateSystemReport(ReportData data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (Document document = new Document(PageSize.A4, 40, 40, 30, 30))
                {
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    writer.CloseStream = false;
                    
                    document.Open();

                    // Fonts - Use fallback if Arial not found
                    BaseFont baseFont;
                    if (!string.IsNullOrEmpty(_fontPath) && File.Exists(_fontPath))
                    {
                        baseFont = BaseFont.CreateFont(_fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    }
                    else
                    {
                        // Fallback to Helvetica (built-in font)
                        baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    }
                    Font titleFont = new Font(baseFont, 24, Font.BOLD, BaseColor.WHITE);
                    Font headerFont = new Font(baseFont, 16, Font.BOLD, PrimaryColor);
                    Font subHeaderFont = new Font(baseFont, 11, Font.BOLD, SecondaryColor);
                    Font normalFont = new Font(baseFont, 10, Font.NORMAL, BaseColor.BLACK);
                    Font boldFont = new Font(baseFont, 10, Font.BOLD, BaseColor.BLACK);
                    Font smallFont = new Font(baseFont, 8, Font.NORMAL, BaseColor.GRAY);
                    Font whiteFont = new Font(baseFont, 10, Font.BOLD, BaseColor.WHITE);
                    Font largeBoldFont = new Font(baseFont, 14, Font.BOLD, PrimaryColor);

                    // Modern Header with gradient effect (simulated with colored cell)
                    PdfPTable headerTable = new PdfPTable(1);
                    headerTable.WidthPercentage = 100;
                    headerTable.SpacingAfter = 20f;
                    
                    PdfPCell headerCell = new PdfPCell(new Phrase("📊 ETÜT SİSTEMİ RAPORU", titleFont));
                    headerCell.BackgroundColor = PrimaryColor;
                    headerCell.Padding = 20f;
                    headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    headerCell.Border = Rectangle.NO_BORDER;
                    headerTable.AddCell(headerCell);
                    
                    PdfPCell dateCell = new PdfPCell(new Phrase(
                        $"📅 {data.StartDate:dd.MM.yyyy} - {data.EndDate:dd.MM.yyyy} • {DateTime.Now:dd.MM.yyyy HH:mm}", 
                        new Font(baseFont, 9, Font.NORMAL, BaseColor.WHITE)));
                    dateCell.BackgroundColor = SecondaryColor;
                    dateCell.Padding = 10f;
                    dateCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    dateCell.Border = Rectangle.NO_BORDER;
                    headerTable.AddCell(dateCell);
                    
                    document.Add(headerTable);

                    // Statistics Cards (4x2 grid)
                    PdfPTable statsGrid = new PdfPTable(4);
                    statsGrid.WidthPercentage = 100;
                    statsGrid.SpacingAfter = 15f;
                    
                    AddStatCard(statsGrid, "👨‍🎓 Toplam Öğrenci", data.TotalStudents.ToString(), baseFont, VeryLightColor, PrimaryColor);
                    AddStatCard(statsGrid, "👨‍🏫 Toplam Öğretmen", data.TotalTeachers.ToString(), baseFont, VeryLightColor, PrimaryColor);
                    AddStatCard(statsGrid, "🏫 Toplam Derslik", data.TotalClassrooms.ToString(), baseFont, VeryLightColor, PrimaryColor);
                    AddStatCard(statsGrid, "📚 Toplam Etüt", data.TotalSessions.ToString(), baseFont, VeryLightColor, PrimaryColor);
                    
                    AddStatCard(statsGrid, "✅ Tamamlanan", data.AttendedSessions.ToString(), baseFont, VeryLightColor, SuccessColor);
                    AddStatCard(statsGrid, "⏳ Bekleyen", data.PendingSessions.ToString(), baseFont, VeryLightColor, WarningColor);
                    AddStatCard(statsGrid, "❌ İptal", data.CancelledSessions.ToString(), baseFont, VeryLightColor, DangerColor);
                    AddStatCard(statsGrid, "📊 Katılım Oranı", $"{data.AttendanceRate:F1}%", baseFont, VeryLightColor, PrimaryColor);
                    
                    document.Add(statsGrid);

                    // Divider
                    LineSeparator line = new LineSeparator(2f, 100f, LightColor, Element.ALIGN_CENTER, -2);
                    document.Add(new Chunk(line));
                    document.Add(new Paragraph("\n"));

                    // TOP 10 STUDENTS
                    if (data.TopStudents != null && data.TopStudents.Any())
                    {
                        Paragraph studentsTitle = new Paragraph("🏆 EN AKTİF 10 ÖĞRENCİ", headerFont);
                        studentsTitle.SpacingBefore = 5f;
                        studentsTitle.SpacingAfter = 10f;
                        document.Add(studentsTitle);

                        PdfPTable studentsTable = new PdfPTable(6);
                        studentsTable.WidthPercentage = 100;
                        studentsTable.SetWidths(new float[] { 1f, 3f, 2f, 1.5f, 1.5f, 2f });
                        studentsTable.SpacingAfter = 15f;

                        // Headers
                        AddHeaderCell(studentsTable, "🏅", whiteFont, PrimaryColor);
                        AddHeaderCell(studentsTable, "👤 Ad Soyad", whiteFont, PrimaryColor);
                        AddHeaderCell(studentsTable, "📖 Sınıf", whiteFont, PrimaryColor);
                        AddHeaderCell(studentsTable, "📚 Toplam", whiteFont, PrimaryColor);
                        AddHeaderCell(studentsTable, "✅ Katılım", whiteFont, PrimaryColor);
                        AddHeaderCell(studentsTable, "📊 Oran", whiteFont, PrimaryColor);

                        // Data
                        int rank = 1;
                        foreach (var student in data.TopStudents.Take(10))
                        {
                            BaseColor rankColor = rank == 1 ? GoldColor : rank == 2 ? SilverColor : rank == 3 ? BronzeColor : PrimaryColor;
                            AddRankCell(studentsTable, rank.ToString(), baseFont, rankColor);
                            AddDataCell(studentsTable, student.FullName, boldFont, BaseColor.WHITE);
                            AddDataCell(studentsTable, student.ClassName ?? "-", normalFont, BaseColor.WHITE);
                            AddDataCell(studentsTable, student.TotalSessions.ToString(), boldFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                            AddDataCell(studentsTable, student.AttendedSessions.ToString(), normalFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                            AddPercentCell(studentsTable, $"{student.AttendanceRate:F1}%", whiteFont, PrimaryColor);
                            rank++;
                        }

                        document.Add(studentsTable);
                    }

                    document.Add(new Chunk(line));
                    document.Add(new Paragraph("\n"));

                    // TOP 10 TEACHERS
                    if (data.TopTeachers != null && data.TopTeachers.Any())
                    {
                        Paragraph teachersTitle = new Paragraph("👨‍🏫 EN AKTİF 10 ÖĞRETMEN", headerFont);
                        teachersTitle.SpacingBefore = 5f;
                        teachersTitle.SpacingAfter = 10f;
                        document.Add(teachersTitle);

                        PdfPTable teachersTable = new PdfPTable(6);
                        teachersTable.WidthPercentage = 100;
                        teachersTable.SetWidths(new float[] { 1f, 3f, 2f, 1.5f, 1.5f, 2f });
                        teachersTable.SpacingAfter = 15f;

                        // Headers
                        AddHeaderCell(teachersTable, "🏅", whiteFont, PrimaryColor);
                        AddHeaderCell(teachersTable, "👤 Ad Soyad", whiteFont, PrimaryColor);
                        AddHeaderCell(teachersTable, "📚 Branş", whiteFont, PrimaryColor);
                        AddHeaderCell(teachersTable, "📋 Toplam", whiteFont, PrimaryColor);
                        AddHeaderCell(teachersTable, "✅ Tamamlanan", whiteFont, PrimaryColor);
                        AddHeaderCell(teachersTable, "📊 Başarı", whiteFont, PrimaryColor);

                        // Data
                        int rank = 1;
                        foreach (var teacher in data.TopTeachers.Take(10))
                        {
                            BaseColor rankColor = rank == 1 ? GoldColor : rank == 2 ? SilverColor : rank == 3 ? BronzeColor : PrimaryColor;
                            AddRankCell(teachersTable, rank.ToString(), baseFont, rankColor);
                            AddDataCell(teachersTable, teacher.FullName, boldFont, BaseColor.WHITE);
                            AddDataCell(teachersTable, teacher.Branch ?? "-", normalFont, BaseColor.WHITE);
                            AddDataCell(teachersTable, teacher.TotalSessions.ToString(), boldFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                            AddDataCell(teachersTable, teacher.CompletedSessions.ToString(), normalFont, BaseColor.WHITE, Element.ALIGN_CENTER);
                            AddPercentCell(teachersTable, $"{teacher.SuccessRate:F1}%", whiteFont, PrimaryColor);
                            rank++;
                        }

                        document.Add(teachersTable);
                    }

                    // Footer
                    document.Add(new Paragraph("\n"));
                    document.Add(new Chunk(line));
                    Paragraph footer = new Paragraph(
                        $"Bu rapor {DateTime.Now:dd.MM.yyyy HH:mm} tarihinde otomatik oluşturulmuştur. • ETÜT SİSTEMİ © 2025",
                        smallFont
                    );
                    footer.Alignment = Element.ALIGN_CENTER;
                    footer.SpacingBefore = 10f;
                    document.Add(footer);

                    document.Close();
                }

                return ms.ToArray();
            }
        }

        private void AddStatCard(PdfPTable table, string label, string value, BaseFont baseFont, BaseColor bgColor, BaseColor valueColor)
        {
            PdfPCell cell = new PdfPCell();
            cell.BackgroundColor = bgColor;
            cell.Border = Rectangle.LEFT_BORDER;
            cell.BorderColor = valueColor;
            cell.BorderWidth = 3f;
            cell.Padding = 10f;
            cell.PaddingLeft = 12f;
            
            Paragraph p = new Paragraph();
            p.Add(new Chunk(label + "\n", new Font(baseFont, 8, Font.NORMAL, valueColor)));
            p.Add(new Chunk(value, new Font(baseFont, 16, Font.BOLD, valueColor)));
            cell.AddElement(p);
            
            table.AddCell(cell);
        }

        private void AddHeaderCell(PdfPTable table, string text, Font font, BaseColor bgColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bgColor;
            cell.Padding = 10f;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Border = Rectangle.NO_BORDER;
            table.AddCell(cell);
        }

        private void AddRankCell(PdfPTable table, string text, BaseFont baseFont, BaseColor bgColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, new Font(baseFont, 10, Font.BOLD, BaseColor.WHITE)));
            cell.BackgroundColor = bgColor;
            cell.Padding = 10f;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Border = Rectangle.NO_BORDER;
            table.AddCell(cell);
        }

        private void AddDataCell(PdfPTable table, string text, Font font, BaseColor bgColor, int alignment = Element.ALIGN_LEFT)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bgColor;
            cell.Padding = 10f;
            cell.HorizontalAlignment = alignment;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Border = Rectangle.BOTTOM_BORDER;
            cell.BorderColor = LightColor;
            cell.BorderWidth = 1f;
            table.AddCell(cell);
        }

        private void AddPercentCell(PdfPTable table, string text, Font font, BaseColor bgColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.BackgroundColor = bgColor;
            cell.Padding = 8f;
            cell.HorizontalAlignment = Element.ALIGN_CENTER;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
            cell.Border = Rectangle.NO_BORDER;
            table.AddCell(cell);
        }
    }

    // Data models for report
    public class ReportData
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClassrooms { get; set; }
        public int TotalSessions { get; set; }
        public int AttendedSessions { get; set; }
        public int PendingSessions { get; set; }
        public int CancelledSessions { get; set; }
        public double AttendanceRate { get; set; }
        public List<TopStudentItem> TopStudents { get; set; }
        public List<TopTeacherItem> TopTeachers { get; set; }
    }

    public class TopStudentItem
    {
        public string FullName { get; set; }
        public string ClassName { get; set; }
        public int TotalSessions { get; set; }
        public int AttendedSessions { get; set; }
        public double AttendanceRate { get; set; }
    }

    public class TopTeacherItem
    {
        public string FullName { get; set; }
        public string Branch { get; set; }
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public double SuccessRate { get; set; }
    }

    // Legacy models for backward compatibility
    public class StudentReportItem
    {
        public string FullName { get; set; }
        public string ClassName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TeacherReportItem
    {
        public string FullName { get; set; }
        public string Branch { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}