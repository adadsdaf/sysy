using POSSystem.Database;
using POSSystem.Models;
using System.Data;

namespace POSSystem.Services
{
    public static class EmployeeService
    {
        public static List<Employee> GetAll(string? status = null)
        {
            var sql = "SELECT * FROM Employees WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (!string.IsNullOrEmpty(status)) { sql += " AND Status=@St"; p["@St"] = status; }
            sql += " ORDER BY Name";
            return DatabaseHelper.ExecuteQuery(sql, p).Rows.Cast<DataRow>().Select(MapEmployee).ToList();
        }

        public static Employee? GetById(int id)
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM Employees WHERE Id=@Id", new() { ["@Id"] = id });
            return dt.Rows.Count > 0 ? MapEmployee(dt.Rows[0]) : null;
        }

        public static int Create(Employee e)
        {
            var id = DatabaseHelper.ExecuteScalar(@"
INSERT INTO Employees(Name,NationalId,Phone,Position,Department,BaseSalary,HousingAllowance,TransportAllowance,OtherAllowances,HireDate,Status,Notes)
VALUES(@N,@NI,@Ph,@Pos,@Dep,@Sal,@Hou,@Tra,@Oth,@HD,@St,@No);SELECT SCOPE_IDENTITY();",
                new() {
                    ["@N"]=e.Name, ["@NI"]=(object?)e.NationalId??DBNull.Value,
                    ["@Ph"]=(object?)e.Phone??DBNull.Value, ["@Pos"]=(object?)e.Position??DBNull.Value,
                    ["@Dep"]=(object?)e.Department??DBNull.Value, ["@Sal"]=e.BaseSalary,
                    ["@Hou"]=e.HousingAllowance, ["@Tra"]=e.TransportAllowance,
                    ["@Oth"]=e.OtherAllowances, ["@HD"]=e.HireDate.Date,
                    ["@St"]=e.Status, ["@No"]=(object?)e.Notes??DBNull.Value });
            return Convert.ToInt32(id);
        }

        public static void Update(Employee e)
        {
            DatabaseHelper.ExecuteNonQuery(@"
UPDATE Employees SET Name=@N,NationalId=@NI,Phone=@Ph,Position=@Pos,Department=@Dep,
BaseSalary=@Sal,HousingAllowance=@Hou,TransportAllowance=@Tra,OtherAllowances=@Oth,
HireDate=@HD,TerminationDate=@TD,Status=@St,Notes=@No WHERE Id=@Id",
                new() {
                    ["@N"]=e.Name, ["@NI"]=(object?)e.NationalId??DBNull.Value,
                    ["@Ph"]=(object?)e.Phone??DBNull.Value, ["@Pos"]=(object?)e.Position??DBNull.Value,
                    ["@Dep"]=(object?)e.Department??DBNull.Value, ["@Sal"]=e.BaseSalary,
                    ["@Hou"]=e.HousingAllowance, ["@Tra"]=e.TransportAllowance,
                    ["@Oth"]=e.OtherAllowances, ["@HD"]=e.HireDate.Date,
                    ["@TD"]=e.TerminationDate.HasValue?(object)e.TerminationDate.Value.Date:DBNull.Value,
                    ["@St"]=e.Status, ["@No"]=(object?)e.Notes??DBNull.Value, ["@Id"]=e.Id });
        }

        public static void Delete(int id) =>
            DatabaseHelper.ExecuteNonQuery("DELETE FROM Employees WHERE Id=@Id", new() { ["@Id"]=id });

        // ── Violations ──────────────────────────────────────────────────
        public static List<EmployeeViolation> GetViolations(int? employeeId = null)
        {
            var sql = @"SELECT v.*, e.Name AS EmployeeName FROM EmployeeViolations v
                        JOIN Employees e ON v.EmployeeId=e.Id WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (employeeId.HasValue) { sql += " AND v.EmployeeId=@EId"; p["@EId"] = employeeId; }
            sql += " ORDER BY v.ViolationDate DESC";
            return DatabaseHelper.ExecuteQuery(sql, p).Rows.Cast<DataRow>().Select(r => new EmployeeViolation
            {
                Id=(int)r["Id"], EmployeeId=(int)r["EmployeeId"],
                EmployeeName=r["EmployeeName"]==DBNull.Value?null:(string)r["EmployeeName"],
                ViolationDate=(DateTime)r["ViolationDate"],
                ViolationType=(string)r["ViolationType"],
                Description=r["Description"]==DBNull.Value?"":(string)r["Description"],
                Deduction=(decimal)r["Deduction"],
                Status=(string)r["Status"],
                Notes=r["Notes"]==DBNull.Value?null:(string)r["Notes"],
                CreatedAt=(DateTime)r["CreatedAt"]
            }).ToList();
        }

        public static int CreateViolation(EmployeeViolation v)
        {
            var id = DatabaseHelper.ExecuteScalar(@"
INSERT INTO EmployeeViolations(EmployeeId,ViolationDate,ViolationType,Description,Deduction,Status,Notes)
VALUES(@EId,@VD,@VT,@Desc,@Ded,@St,@No);SELECT SCOPE_IDENTITY();",
                new() { ["@EId"]=v.EmployeeId, ["@VD"]=v.ViolationDate.Date, ["@VT"]=v.ViolationType,
                    ["@Desc"]=(object?)v.Description??DBNull.Value, ["@Ded"]=v.Deduction,
                    ["@St"]=v.Status, ["@No"]=(object?)v.Notes??DBNull.Value });
            return Convert.ToInt32(id);
        }

        public static void UpdateViolationStatus(int id, string status) =>
            DatabaseHelper.ExecuteNonQuery("UPDATE EmployeeViolations SET Status=@St WHERE Id=@Id",
                new() { ["@St"]=status, ["@Id"]=id });

        public static void DeleteViolation(int id) =>
            DatabaseHelper.ExecuteNonQuery("DELETE FROM EmployeeViolations WHERE Id=@Id", new() { ["@Id"]=id });

        // ── Salaries ─────────────────────────────────────────────────────
        public static List<EmployeeSalary> GetSalaries(int? year = null, int? month = null, int? employeeId = null)
        {
            var sql = @"SELECT s.*, e.Name AS EmployeeName FROM EmployeeSalaries s
                        JOIN Employees e ON s.EmployeeId=e.Id WHERE 1=1";
            var p = new Dictionary<string, object?>();
            if (year.HasValue) { sql += " AND s.Year=@Yr"; p["@Yr"]=year; }
            if (month.HasValue) { sql += " AND s.Month=@Mo"; p["@Mo"]=month; }
            if (employeeId.HasValue) { sql += " AND s.EmployeeId=@EId"; p["@EId"]=employeeId; }
            sql += " ORDER BY s.Year DESC, s.Month DESC, e.Name";
            return DatabaseHelper.ExecuteQuery(sql, p).Rows.Cast<DataRow>().Select(r => new EmployeeSalary
            {
                Id=(int)r["Id"], EmployeeId=(int)r["EmployeeId"],
                EmployeeName=r["EmployeeName"]==DBNull.Value?null:(string)r["EmployeeName"],
                Month=(int)r["Month"], Year=(int)r["Year"],
                BaseSalary=(decimal)r["BaseSalary"], Allowances=(decimal)r["Allowances"],
                Bonuses=(decimal)r["Bonuses"], Deductions=(decimal)r["Deductions"],
                ViolationDeductions=(decimal)r["ViolationDeductions"],
                Status=(string)r["Status"],
                PaidDate=r["PaidDate"]==DBNull.Value?null:(DateTime?)r["PaidDate"],
                Notes=r["Notes"]==DBNull.Value?null:(string)r["Notes"],
                CreatedAt=(DateTime)r["CreatedAt"]
            }).ToList();
        }

        public static void GenerateSalaries(int month, int year)
        {
            var employees = GetAll("active");
            foreach (var emp in employees)
            {
                var exists = DatabaseHelper.ExecuteScalar("SELECT COUNT(*) FROM EmployeeSalaries WHERE EmployeeId=@E AND Month=@M AND Year=@Y",
                    new() { ["@E"]=emp.Id, ["@M"]=month, ["@Y"]=year });
                if (Convert.ToInt32(exists) > 0) continue;

                var totalViolDed = DatabaseHelper.ExecuteScalar(@"
SELECT ISNULL(SUM(Deduction),0) FROM EmployeeViolations
WHERE EmployeeId=@E AND Status='approved'
AND MONTH(ViolationDate)=@M AND YEAR(ViolationDate)=@Y",
                    new() { ["@E"]=emp.Id, ["@M"]=month, ["@Y"]=year });

                DatabaseHelper.ExecuteNonQuery(@"
INSERT INTO EmployeeSalaries(EmployeeId,Month,Year,BaseSalary,Allowances,Bonuses,Deductions,ViolationDeductions,Status)
VALUES(@E,@M,@Y,@BS,@Al,0,0,@VD,'pending')",
                    new() { ["@E"]=emp.Id, ["@M"]=month, ["@Y"]=year,
                        ["@BS"]=emp.BaseSalary,
                        ["@Al"]=emp.HousingAllowance+emp.TransportAllowance+emp.OtherAllowances,
                        ["@VD"]=Convert.ToDecimal(totalViolDed) });
            }
        }

        public static void PaySalary(int salaryId)
        {
            DatabaseHelper.ExecuteNonQuery("UPDATE EmployeeSalaries SET Status='paid',PaidDate=GETDATE() WHERE Id=@Id",
                new() { ["@Id"]=salaryId });
        }

        public static void UpdateSalary(EmployeeSalary s)
        {
            DatabaseHelper.ExecuteNonQuery(@"UPDATE EmployeeSalaries SET BaseSalary=@BS,Allowances=@Al,Bonuses=@Bo,
Deductions=@De,ViolationDeductions=@VD,Notes=@No WHERE Id=@Id",
                new() { ["@BS"]=s.BaseSalary, ["@Al"]=s.Allowances, ["@Bo"]=s.Bonuses,
                    ["@De"]=s.Deductions, ["@VD"]=s.ViolationDeductions,
                    ["@No"]=(object?)s.Notes??DBNull.Value, ["@Id"]=s.Id });
        }

        // ── Account Statement ─────────────────────────────────────────────
        public static List<AccountStatement> GetEmployeeStatement(int employeeId, DateTime from, DateTime to)
        {
            var rows = new List<AccountStatement>();
            var sals = GetSalaries(employeeId: employeeId)
                .Where(s => s.CreatedAt >= from && s.CreatedAt <= to.AddDays(1)).ToList();
            decimal balance = 0;
            foreach (var s in sals.OrderBy(x => x.Year).ThenBy(x => x.Month))
            {
                balance += s.NetSalary;
                rows.Add(new AccountStatement
                {
                    Date = s.PaidDate ?? new DateTime(s.Year, s.Month, 1),
                    Description = $"راتب {s.Month}/{s.Year}",
                    Type = "credit", Credit = s.NetSalary, Balance = balance,
                    Reference = $"راتب رقم {s.Id}"
                });
            }
            return rows;
        }

        private static Employee MapEmployee(DataRow r) => new Employee
        {
            Id=(int)r["Id"], Name=(string)r["Name"],
            NationalId=r["NationalId"]==DBNull.Value?null:(string)r["NationalId"],
            Phone=r["Phone"]==DBNull.Value?null:(string)r["Phone"],
            Position=r["Position"]==DBNull.Value?null:(string)r["Position"],
            Department=r["Department"]==DBNull.Value?null:(string)r["Department"],
            BaseSalary=(decimal)r["BaseSalary"], HousingAllowance=(decimal)r["HousingAllowance"],
            TransportAllowance=(decimal)r["TransportAllowance"], OtherAllowances=(decimal)r["OtherAllowances"],
            HireDate=(DateTime)r["HireDate"],
            TerminationDate=r["TerminationDate"]==DBNull.Value?null:(DateTime?)r["TerminationDate"],
            Status=(string)r["Status"],
            Notes=r["Notes"]==DBNull.Value?null:(string)r["Notes"],
            CreatedAt=(DateTime)r["CreatedAt"]
        };
    }
}
