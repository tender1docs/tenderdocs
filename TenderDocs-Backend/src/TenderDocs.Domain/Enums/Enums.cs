namespace TenderDocs.Domain.Enums;

// Access roles, resolved to permission sets by Domain.Authorization.RolePermissions.
//   Admin    = the only unrestricted role (entire app + administration portal).
//   Uploader = document work only (upload / edit / organize / assign to projects).
//   Approver = approval only (review / approve / reject) — no upload, edit, or delete.
//   Viewer   = read-only (view / search / download).
// Approver/Uploader/Viewer keep their original int values; Admin is appended as 3 so existing
// rows are unaffected. Roles are NOT checked directly — authorization is permission-based.
public enum UserRole { Approver = 0, Uploader = 1, Viewer = 2, Admin = 3 }

// Document review state, set by an Approver. New documents start Pending.
public enum DocumentApprovalStatus { Pending = 0, Approved = 1, Rejected = 2 }

// Requirement categories shown on the frontend. Existing int values are preserved for
// already-stored rows; new categories are appended so no data migration is required.
//   GST, PAN, IT Returns, MSME, ISO, Experience Certificates, Bank Statements,
//   Financial Documents, Technical Documents, Others
public enum DocumentType
{
    Gst = 0,
    Pan = 1,
    Itr = 2,
    Msme = 3,
    BalanceSheet = 4,          // legacy — grouped under Financial
    TenderForm = 5,            // legacy — grouped under Technical
    Iso = 6,
    ExperienceCertificate = 7,
    BankStatement = 8,
    FinancialDocument = 9,
    TechnicalDocument = 10,
    Other = 99
}

// Derived from expiry date. Surface labels: Valid / Expiring / Expired / No Expiry
public enum DocumentStatus { Valid = 0, ExpiringSoon = 1, Expired = 2, NoExpiry = 3 }

public enum StorageProviderType { Local = 0, GoogleDrive = 1, S3 = 2 }

public enum NotificationType { DocumentExpiring = 0, DocumentExpired = 1, DocumentUploaded = 2, ProjectShared = 3, System = 99 }

public enum AuditAction { Create = 0, Update = 1, Delete = 2, Download = 3, Login = 4, Upload = 5, Assign = 6, Export = 7 }
