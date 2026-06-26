namespace POSSystem.Database
{
    public static class SchemaSetup
    {
        public static void CreateTablesIfNotExist()
        {
            string sql = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Categories' AND xtype='U')
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Color NVARCHAR(50) DEFAULT '#4CAF50',
    SortOrder INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Items' AND xtype='U')
CREATE TABLE Items (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(300) NOT NULL,
    NameAr NVARCHAR(300),
    Barcode NVARCHAR(100),
    ItemNumber NVARCHAR(100),
    Price DECIMAL(12,2) NOT NULL DEFAULT 0,
    TaxRate DECIMAL(5,2) DEFAULT 0,
    DiscountRate DECIMAL(5,2) DEFAULT 0,
    CategoryId INT REFERENCES Categories(Id),
    Stock DECIMAL(12,3) DEFAULT 0,
    MinStock DECIMAL(12,3) DEFAULT 0,
    Unit NVARCHAR(30) DEFAULT N'قطعة',
    IsFavorite BIT DEFAULT 0,
    IsAvailable BIT DEFAULT 1,
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Customers' AND xtype='U')
CREATE TABLE Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(300) NOT NULL,
    Phone NVARCHAR(50),
    Email NVARCHAR(200),
    Notes NVARCHAR(500),
    TotalPurchases DECIMAL(14,2) DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Role NVARCHAR(50) DEFAULT 'cashier',
    Pin NVARCHAR(20) NOT NULL,
    IsActive BIT DEFAULT 1,
    CanAccessPOS BIT DEFAULT 1,
    CanManageItems BIT DEFAULT 0,
    CanViewReports BIT DEFAULT 0,
    CanApplyDiscount BIT DEFAULT 0,
    CanCancelInvoice BIT DEFAULT 0,
    CanAccessManagement BIT DEFAULT 0,
    CanManageCustomers BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Invoices' AND xtype='U')
CREATE TABLE Invoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNumber NVARCHAR(50) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'open',
    OrderType NVARCHAR(30) DEFAULT 'dine_in',
    TableNumber NVARCHAR(20),
    CustomerId INT REFERENCES Customers(Id),
    CashierId INT REFERENCES Users(Id),
    DiscountAmount DECIMAL(12,2) DEFAULT 0,
    ServiceAmount DECIMAL(12,2) DEFAULT 0,
    AmountPaid DECIMAL(12,2) DEFAULT 0,
    PaymentMethod NVARCHAR(30),
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE(),
    PaidAt DATETIME
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='InvoiceItems' AND xtype='U')
CREATE TABLE InvoiceItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId INT NOT NULL REFERENCES Invoices(Id) ON DELETE CASCADE,
    ItemId INT NOT NULL,
    Name NVARCHAR(300) NOT NULL,
    ItemNumber NVARCHAR(100),
    CategoryName NVARCHAR(200),
    Price DECIMAL(12,2) NOT NULL,
    Quantity DECIMAL(10,3) DEFAULT 1,
    TaxRate DECIMAL(5,2) DEFAULT 0,
    DiscountRate DECIMAL(5,2) DEFAULT 0,
    Notes NVARCHAR(500)
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppSettings' AND xtype='U')
CREATE TABLE AppSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StoreName NVARCHAR(300) DEFAULT N'نقطة المبيعات',
    StoreNameAr NVARCHAR(300),
    Address NVARCHAR(500),
    Phone NVARCHAR(50),
    TaxRate DECIMAL(5,2) DEFAULT 15,
    ServiceRate DECIMAL(5,2) DEFAULT 0,
    Currency NVARCHAR(10) DEFAULT 'SAR',
    CurrencySymbol NVARCHAR(10) DEFAULT N'ر.س',
    ReceiptFooter NVARCHAR(500),
    ReceiptNotes NVARCHAR(500),
    LogoPath NVARCHAR(500),
    PrintCustomerCopy BIT DEFAULT 1,
    PrintKitchenSlips BIT DEFAULT 1,
    LicenseStatus NVARCHAR(20) DEFAULT 'active'
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Employees' AND xtype='U')
CREATE TABLE Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(300) NOT NULL,
    NationalId NVARCHAR(50),
    Phone NVARCHAR(50),
    Position NVARCHAR(200),
    Department NVARCHAR(200),
    BaseSalary DECIMAL(12,2) DEFAULT 0,
    HousingAllowance DECIMAL(12,2) DEFAULT 0,
    TransportAllowance DECIMAL(12,2) DEFAULT 0,
    OtherAllowances DECIMAL(12,2) DEFAULT 0,
    HireDate DATE NOT NULL DEFAULT GETDATE(),
    TerminationDate DATE,
    Status NVARCHAR(20) DEFAULT 'active',
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EmployeeViolations' AND xtype='U')
CREATE TABLE EmployeeViolations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL REFERENCES Employees(Id),
    ViolationDate DATE NOT NULL DEFAULT GETDATE(),
    ViolationType NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    Deduction DECIMAL(12,2) DEFAULT 0,
    Status NVARCHAR(20) DEFAULT 'pending',
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='EmployeeSalaries' AND xtype='U')
CREATE TABLE EmployeeSalaries (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL REFERENCES Employees(Id),
    Month INT NOT NULL,
    Year INT NOT NULL,
    BaseSalary DECIMAL(12,2) DEFAULT 0,
    Allowances DECIMAL(12,2) DEFAULT 0,
    Bonuses DECIMAL(12,2) DEFAULT 0,
    Deductions DECIMAL(12,2) DEFAULT 0,
    ViolationDeductions DECIMAL(12,2) DEFAULT 0,
    Status NVARCHAR(20) DEFAULT 'pending',
    PaidDate DATETIME,
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Suppliers' AND xtype='U')
CREATE TABLE Suppliers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(300) NOT NULL,
    Phone NVARCHAR(50),
    Email NVARCHAR(200),
    Address NVARCHAR(500),
    TaxNumber NVARCHAR(100),
    ContactPerson NVARCHAR(200),
    Balance DECIMAL(14,2) DEFAULT 0,
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PurchaseOrders' AND xtype='U')
CREATE TABLE PurchaseOrders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderNumber NVARCHAR(50) NOT NULL,
    SupplierId INT NOT NULL REFERENCES Suppliers(Id),
    OrderDate DATE NOT NULL DEFAULT GETDATE(),
    ReceiveDate DATE,
    Status NVARCHAR(20) DEFAULT 'pending',
    Total DECIMAL(14,2) DEFAULT 0,
    AmountPaid DECIMAL(14,2) DEFAULT 0,
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PurchaseOrderItems' AND xtype='U')
CREATE TABLE PurchaseOrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseOrderId INT NOT NULL REFERENCES PurchaseOrders(Id) ON DELETE CASCADE,
    ItemId INT NOT NULL,
    ItemName NVARCHAR(300) NOT NULL,
    Unit NVARCHAR(30),
    Quantity DECIMAL(12,3) DEFAULT 1,
    UnitPrice DECIMAL(12,2) DEFAULT 0
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='StockMovements' AND xtype='U')
CREATE TABLE StockMovements (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ItemId INT NOT NULL,
    ItemName NVARCHAR(300),
    MovementType NVARCHAR(20) NOT NULL DEFAULT 'in',
    Quantity DECIMAL(12,3) NOT NULL,
    UnitCost DECIMAL(12,2) DEFAULT 0,
    ReferenceId INT,
    ReferenceType NVARCHAR(50),
    Notes NVARCHAR(500),
    CreatedBy INT REFERENCES Users(Id),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ReturnInvoices' AND xtype='U')
CREATE TABLE ReturnInvoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReturnNumber NVARCHAR(50) NOT NULL,
    OriginalInvoiceId INT,
    OriginalInvoiceNumber NVARCHAR(50),
    CustomerId INT REFERENCES Customers(Id),
    CashierId INT REFERENCES Users(Id),
    Total DECIMAL(12,2) DEFAULT 0,
    Reason NVARCHAR(500),
    RefundMethod NVARCHAR(30) DEFAULT 'cash',
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ReturnInvoiceItems' AND xtype='U')
CREATE TABLE ReturnInvoiceItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReturnInvoiceId INT NOT NULL REFERENCES ReturnInvoices(Id) ON DELETE CASCADE,
    ItemId INT NOT NULL,
    ItemName NVARCHAR(300) NOT NULL,
    Quantity DECIMAL(10,3) DEFAULT 1,
    UnitPrice DECIMAL(12,2) DEFAULT 0,
    Reason NVARCHAR(300)
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CashBoxes' AND xtype='U')
CREATE TABLE CashBoxes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500),
    Balance DECIMAL(14,2) DEFAULT 0,
    IsDefault BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CashVouchers' AND xtype='U')
CREATE TABLE CashVouchers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    VoucherNumber NVARCHAR(50) NOT NULL,
    VoucherType NVARCHAR(20) NOT NULL DEFAULT 'payment',
    CashBoxId INT NOT NULL REFERENCES CashBoxes(Id),
    Amount DECIMAL(14,2) NOT NULL,
    PayeeName NVARCHAR(300) NOT NULL,
    PayeeType NVARCHAR(50),
    PayeeId INT,
    Description NVARCHAR(500) NOT NULL,
    ReferenceNumber NVARCHAR(100),
    VoucherDate DATE NOT NULL DEFAULT GETDATE(),
    CreatedBy INT REFERENCES Users(Id),
    Notes NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE()
);
";
            DatabaseHelper.ExecuteNonQuery(sql);
            MigrateExistingTables();
            SeedInitialData();
        }

        private static void MigrateExistingTables()
        {
            var migrations = new[]
            {
                ("Users", "CanAccessPOS BIT DEFAULT 1"),
                ("Users", "CanManageItems BIT DEFAULT 0"),
                ("Users", "CanViewReports BIT DEFAULT 0"),
                ("Users", "CanApplyDiscount BIT DEFAULT 0"),
                ("Users", "CanCancelInvoice BIT DEFAULT 0"),
                ("Users", "CanAccessManagement BIT DEFAULT 0"),
                ("Users", "CanManageCustomers BIT DEFAULT 0"),
                ("AppSettings", "ReceiptNotes NVARCHAR(500)"),
                ("AppSettings", "LogoPath NVARCHAR(500)"),
                ("AppSettings", "PrintCustomerCopy BIT DEFAULT 1"),
                ("AppSettings", "PrintKitchenSlips BIT DEFAULT 1"),
                ("InvoiceItems", "ItemNumber NVARCHAR(100)"),
                ("InvoiceItems", "CategoryName NVARCHAR(200)"),
                ("Items", "MinStock DECIMAL(12,3) DEFAULT 0"),
                ("Items", "Unit NVARCHAR(30) DEFAULT N'قطعة'"),
            };

            foreach (var (table, colDef) in migrations)
            {
                var colName = colDef.Split(' ')[0];
                try
                {
                    DatabaseHelper.ExecuteNonQuery($@"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id=OBJECT_ID('{table}') AND name='{colName}')
ALTER TABLE {table} ADD {colDef};");
                }
                catch { }
            }
        }

        private static void SeedInitialData()
        {
            var userCount = DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Users");
            if (Convert.ToInt32(userCount) == 0)
            {
                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO Users (Name,Role,Pin,IsActive,CanAccessPOS,CanManageItems,CanViewReports,CanApplyDiscount,CanCancelInvoice,CanAccessManagement,CanManageCustomers)
VALUES (N'المدير','admin','1234',1,1,1,1,1,1,1,1);
INSERT INTO Users (Name,Role,Pin,IsActive,CanAccessPOS,CanManageItems,CanViewReports,CanApplyDiscount,CanCancelInvoice,CanAccessManagement,CanManageCustomers)
VALUES (N'الكاشير','cashier','0000',1,1,0,0,0,0,0,0);");
            }

            var settingsCount = DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM AppSettings");
            if (Convert.ToInt32(settingsCount) == 0)
            {
                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO AppSettings(StoreName,StoreNameAr,TaxRate,Currency,CurrencySymbol,ReceiptNotes,PrintCustomerCopy,PrintKitchenSlips)
VALUES(N'مطعم النجمة',N'مطعم النجمة',15,'SAR',N'ر.س',N'الطلب لا يمكن استرجاعه أو الغاؤه',1,1);");
            }

            var catCount = DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Categories");
            if (Convert.ToInt32(catCount) == 0)
            {
                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO Categories(Name,Color,SortOrder) VALUES(N'مشروبات','#2196F3',1);
INSERT INTO Categories(Name,Color,SortOrder) VALUES(N'وجبات رئيسية','#4CAF50',2);
INSERT INTO Categories(Name,Color,SortOrder) VALUES(N'بيتزا','#FF9800',3);
INSERT INTO Categories(Name,Color,SortOrder) VALUES(N'سلطات','#8BC34A',4);
INSERT INTO Categories(Name,Color,SortOrder) VALUES(N'حلويات','#E91E63',5);
INSERT INTO Categories(Name,Color,SortOrder) VALUES(N'مقبلات','#9C27B0',6);");
            }

            var itemCount = DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM Items");
            if (Convert.ToInt32(itemCount) == 0)
            {
                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'بيتزا مرغريتا كبير','101',9000,15,3,1,1,50);
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'بيتزا مرغريتا وسط','102',7000,15,3,1,0,50);
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'بيتزا دجاج كبير','103',9000,15,3,1,1,50);
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'بروست دجاج','201',8000,15,2,1,1,100);
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'شاورما دجاج','202',5000,15,2,1,1,100);
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'شاورما لحم','203',6000,15,2,1,1,100);
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'عصير برتقال','301',1800,15,1,1,0,200);
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'عصير مانجو','302',2000,15,1,1,0,200);
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'مياه معدنية','303',1500,15,1,1,1,500);
INSERT INTO Items(Name,ItemNumber,Price,TaxRate,CategoryId,IsAvailable,IsFavorite,Stock) VALUES(N'قهوة عربية','304',2000,15,1,1,1,300);");
            }

            var cbCount = DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM CashBoxes");
            if (Convert.ToInt32(cbCount) == 0)
            {
                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO CashBoxes(Name,Description,Balance,IsDefault) VALUES(N'الصندوق الرئيسي',N'الصندوق النقدي الرئيسي',0,1);
INSERT INTO CashBoxes(Name,Description,Balance,IsDefault) VALUES(N'صندوق المطبخ',N'صندوق المطبخ الخلفي',0,0);");
            }
        }
    }
}
