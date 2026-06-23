using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Documents;

public static class DocumentMapping
{
    public static string Label(DocumentType t) => t switch
    {
        DocumentType.Gst => "GST",
        DocumentType.Pan => "PAN",
        DocumentType.Itr => "IT Returns",
        DocumentType.Msme => "MSME",
        DocumentType.BalanceSheet => "Balance Sheet",
        DocumentType.TenderForm => "Tender Form",
        DocumentType.Iso => "ISO",
        DocumentType.ExperienceCertificate => "Experience Certificates",
        DocumentType.BankStatement => "Bank Statements",
        DocumentType.FinancialDocument => "Financial Documents",
        DocumentType.TechnicalDocument => "Technical Documents",
        _ => "Others"
    };

    /// <summary>The export folder a category lands in. The ZIP has Financial/, Technical/ and Others/.</summary>
    public static string ZipFolder(DocumentType t) => t switch
    {
        DocumentType.TechnicalDocument or DocumentType.ExperienceCertificate
            or DocumentType.TenderForm => "Technical",
        DocumentType.Other => "Others",   // uncategorized
        _ => "Financial"   // GST, PAN, IT Returns, MSME, ISO, Bank Statements, Financial Docs
    };

    /// <summary>The category suffix appended to exported filenames, e.g. "_GST", "_ITReturns", "_Technical".</summary>
    public static string CategoryTag(DocumentType t) => t switch
    {
        DocumentType.Gst => "GST",
        DocumentType.Pan => "PAN",
        DocumentType.Itr => "ITReturns",
        DocumentType.Msme => "MSME",
        DocumentType.Iso => "ISO",
        DocumentType.BankStatement => "BankStatements",
        DocumentType.FinancialDocument or DocumentType.BalanceSheet => "Financial",
        DocumentType.TechnicalDocument or DocumentType.ExperienceCertificate
            or DocumentType.TenderForm => "Technical",
        _ => "Others"
    };

    /// <summary>Every folder a project export must always contain, in display order.</summary>
    public static readonly string[] AllZipFolders = { "Financial", "Technical", "Others" };

    public static DocumentDto ToDto(Document d, int expiringThresholdDays = 30)
        => new(
            d.Id, d.Name, d.DocumentType.ToString(), Label(d.DocumentType),
            d.IssuingAuthority, d.FinancialYear, d.Notes, d.IssueDate, d.ExpiryDate,
            d.ComputeStatus(expiringThresholdDays).ToString(),
            d.StorageProvider.ToString(), d.FileSizeBytes, d.ContentType,
            d.FolderId, d.UploadedById, d.UploadedBy?.FullName, d.CreatedAt,
            d.DocumentTags.Select(t => t.Tag.Name).ToList(),
            d.ApprovalStatus.ToString(), d.ApprovedBy?.FullName, d.ApprovalAt, d.RejectionReason);
}
