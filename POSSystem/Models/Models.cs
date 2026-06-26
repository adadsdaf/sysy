namespace POSSystem.Models
{
    // ── Existing models ──────────────────────────────────────────────────

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#4CAF50";
        public int SortOrder { get; set; }
    }

    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? NameAr { get; set; }
        public string? Barcode { get; set; }
        public string? ItemNumber { get; set; }
        public decimal Price { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountRate { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
        public int? Stock { get; set; }
        public decimal? MinStock { get; set; }
        public string? Unit { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public decimal TotalPurchases { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserPermissions
    {
        public bool CanAccessPOS { get; set; } = true;
        public bool CanManageItems { get; set; } = false;
        public bool CanViewReports { get; set; } = false;
        public bool CanApplyDiscount { get; set; } = false;
        public bool CanCancelInvoice { get; set; } = false;
        public bool CanAccessManagement { get; set; } = false;
        public bool CanManageCustomers { get; set; } = false;
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Role { get; set; } = "cashier";
        public string Pin { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public UserPermissions Permissions { get; set; } = new();
        public bool IsAdmin => Role == "admin";
    }

    public class InvoiceItem
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = "";
        public string? ItemNumber { get; set; }
        public string? CategoryName { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountRate { get; set; }
        public string? Notes { get; set; }
        public decimal Subtotal => Price * Quantity;
        public decimal TaxAmount => Subtotal * (TaxRate / 100);
        public decimal DiscountAmount => Subtotal * (DiscountRate / 100);
        public decimal Total => Subtotal + TaxAmount - DiscountAmount;
    }

    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string Status { get; set; } = "open";
        public string OrderType { get; set; } = "dine_in";
        public string? TableNumber { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? CashierId { get; set; }
        public string? CashierName { get; set; }
        public List<InvoiceItem> Items { get; set; } = new();
        public decimal Subtotal => Items.Sum(i => i.Subtotal);
        public decimal TaxAmount => Items.Sum(i => i.TaxAmount);
        public decimal DiscountAmount { get; set; }
        public decimal ServiceAmount { get; set; }
        public decimal Total => Subtotal + TaxAmount - DiscountAmount + ServiceAmount;
        public decimal AmountPaid { get; set; }
        public decimal Change => AmountPaid - Total;
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class AppSettings
    {
        public int Id { get; set; }
        public string StoreName { get; set; } = "نقطة المبيعات";
        public string? StoreNameAr { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public decimal TaxRate { get; set; } = 15;
        public decimal ServiceRate { get; set; } = 0;
        public string Currency { get; set; } = "SAR";
        public string CurrencySymbol { get; set; } = "ر.س";
        public string? ReceiptFooter { get; set; }
        public string? ReceiptNotes { get; set; }
        public string? LogoPath { get; set; }
        public string LicenseStatus { get; set; } = "active";
        public bool PrintCustomerCopy { get; set; } = true;
        public bool PrintKitchenSlips { get; set; } = true;
    }

    // ── HR Models ────────────────────────────────────────────────────────

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? NationalId { get; set; }
        public string? Phone { get; set; }
        public string? Position { get; set; }
        public string? Department { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal HousingAllowance { get; set; }
        public decimal TransportAllowance { get; set; }
        public decimal OtherAllowances { get; set; }
        public decimal TotalSalary => BaseSalary + HousingAllowance + TransportAllowance + OtherAllowances;
        public DateTime HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string Status { get; set; } = "active"; // active, terminated, suspended
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EmployeeViolation
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime ViolationDate { get; set; }
        public string ViolationType { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Deduction { get; set; }
        public string Status { get; set; } = "pending"; // pending, approved, dismissed
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EmployeeSalary
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Bonuses { get; set; }
        public decimal Deductions { get; set; }
        public decimal ViolationDeductions { get; set; }
        public decimal NetSalary => BaseSalary + Allowances + Bonuses - Deductions - ViolationDeductions;
        public string Status { get; set; } = "pending"; // pending, paid
        public DateTime? PaidDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Supplier & Purchase Models ────────────────────────────────────────

    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public string? ContactPerson { get; set; }
        public decimal Balance { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PurchaseOrder
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ReceiveDate { get; set; }
        public string Status { get; set; } = "pending"; // pending, received, cancelled
        public decimal Total { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance => Total - AmountPaid;
        public string? Notes { get; set; }
        public List<PurchaseOrderItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }

    // ── Inventory / Stock Models ─────────────────────────────────────────

    public class StockMovement
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string MovementType { get; set; } = "in"; // in, out, adjustment, return
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; } // purchase, invoice, manual, return
        public string? Notes { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Returns Models ───────────────────────────────────────────────────

    public class ReturnInvoice
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; } = "";
        public int? OriginalInvoiceId { get; set; }
        public string? OriginalInvoiceNumber { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int? CashierId { get; set; }
        public string? CashierName { get; set; }
        public decimal Total { get; set; }
        public string Reason { get; set; } = "";
        public string RefundMethod { get; set; } = "cash"; // cash, store_credit
        public string? Notes { get; set; }
        public List<ReturnInvoiceItem> Items { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class ReturnInvoiceItem
    {
        public int Id { get; set; }
        public int ReturnInvoiceId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
        public string? Reason { get; set; }
    }

    // ── Finance Models ───────────────────────────────────────────────────

    public class CashBox
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Balance { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CashVoucher
    {
        public int Id { get; set; }
        public string VoucherNumber { get; set; } = "";
        public string VoucherType { get; set; } = "payment"; // payment=صرف, receipt=قبض
        public int CashBoxId { get; set; }
        public string? CashBoxName { get; set; }
        public decimal Amount { get; set; }
        public string PayeeName { get; set; } = "";
        public string? PayeeType { get; set; } // supplier, employee, other
        public int? PayeeId { get; set; }
        public string Description { get; set; } = "";
        public string? ReferenceNumber { get; set; }
        public DateTime VoucherDate { get; set; }
        public int? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Report Models ─────────────────────────────────────────────────────

    public class SalesReport
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal NetSales { get; set; }
    }

    public class ItemSalesReport
    {
        public string ItemName { get; set; } = "";
        public string? ItemNumber { get; set; }
        public string? CategoryName { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class AccountStatement
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public string Type { get; set; } = ""; // debit, credit
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public string? Reference { get; set; }
    }
}
