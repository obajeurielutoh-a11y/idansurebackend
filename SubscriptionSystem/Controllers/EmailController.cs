using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.Interfaces;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using Microsoft.AspNetCore.Authorization;

namespace SubscriptionSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<EmailController> _logger;

        public EmailController(IEmailService emailService, IPaymentRepository paymentRepository, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _paymentRepository = paymentRepository;
            _logger = logger;
        }

        [HttpPost("send-to-premium")]
        //[Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> SendEmailToPremiumSubscribers([FromForm] PremiumEmailModel model)
        {
            try
            {
                string htmlContent = model.Body;

                if (model.File != null && model.File.Length > 0)
                {
                    var fileExtension = Path.GetExtension(model.File.FileName).ToLower();
                    if (fileExtension != ".xlsx" && fileExtension != ".xls")
                    {
                        return BadRequest("Only Excel files (.xlsx or .xls) are allowed.");
                    }

                    try
                    {
                        var attachmentContent = await GetExcelContentAsHtmlTable(model.File);
                        htmlContent += "<br/><br/>" + attachmentContent;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Excel file");
                        return BadRequest($"Error processing Excel file: {ex.Message}");
                    }
                }

                var premiumSubscribers = await _paymentRepository.GetUsersWithPaymentAmountAsync(2100m);

                if (model.File != null && model.File.Length > 0)
                {
                    using var attachmentStream = new MemoryStream();
                    await model.File.CopyToAsync(attachmentStream);
                    var fileBytes = attachmentStream.ToArray();

                    await _emailService.SendBulkEmailWithAttachmentAsync(
                        premiumSubscribers,
                        model.Subject,
                        htmlContent,
                        fileBytes,
                        model.File.FileName,
                        model.File.ContentType
                    );
                }
                else
                {
                    await _emailService.SendBulkEmailAsync(premiumSubscribers, model.Subject, htmlContent);
                }

                return Ok($"Emails sent successfully to {premiumSubscribers.Count} premium subscribers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending emails to premium subscribers");
                return StatusCode(500, $"An error occurred while sending emails: {ex.Message}");
            }
        }

        private async Task<string> GetExcelContentAsHtmlTable(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            IWorkbook workbook;

            try
            {
                if (file.FileName.EndsWith(".xlsx"))
                {
                    workbook = new XSSFWorkbook(stream);
                }
                else
                {
                    workbook = new HSSFWorkbook(stream);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to read the Excel file. It may be corrupted or encrypted.", ex);
            }

            var sheet = workbook.GetSheetAt(0);
            if (sheet == null)
            {
                throw new InvalidOperationException("Excel file does not contain any worksheets.");
            }

            var htmlBuilder = new System.Text.StringBuilder();
            htmlBuilder.Append("<div style='overflow-x: auto;'>");
            htmlBuilder.Append("<table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>");

            // Add header row
            htmlBuilder.Append("<thead><tr>");
            var headerRow = sheet.GetRow(0);
            if (headerRow != null)
            {
                for (int col = 0; col < headerRow.LastCellNum; col++)
                {
                    htmlBuilder.Append("<th style='border: 1px solid #ddd; padding: 8px; background-color: #f2f2f2; text-align: left;'>");
                    htmlBuilder.Append(headerRow.GetCell(col)?.ToString() ?? "");
                    htmlBuilder.Append("</th>");
                }
            }
            htmlBuilder.Append("</tr></thead>");

            // Add data rows
            htmlBuilder.Append("<tbody>");
            for (int row = 1; row <= sheet.LastRowNum; row++)
            {
                var dataRow = sheet.GetRow(row);
                if (dataRow != null)
                {
                    htmlBuilder.Append("<tr>");
                    for (int col = 0; col < headerRow.LastCellNum; col++)
                    {
                        htmlBuilder.Append("<td style='border: 1px solid #ddd; padding: 8px;'>");
                        htmlBuilder.Append(dataRow.GetCell(col)?.ToString() ?? "");
                        htmlBuilder.Append("</td>");
                    }
                    htmlBuilder.Append("</tr>");
                }
            }
            htmlBuilder.Append("</tbody>");
            htmlBuilder.Append("</table>");
            htmlBuilder.Append("</div>");

            return htmlBuilder.ToString();
        }
    }

    public class PremiumEmailModel
    {
        public string Subject { get; set; }
        public string Body { get; set; }
        public IFormFile File { get; set; }
    }
}

