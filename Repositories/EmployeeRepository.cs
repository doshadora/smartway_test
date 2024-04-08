using Dapper;
using Npgsql;
using Smartway.Helpers;
using Smartway.Models;
using Smartway.Services;
using System.Data;
using System.Text;
using System.Transactions;

namespace Smartway.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ILogger<EmployeeRepository> _logger;
        private readonly IDbService _dbService;

        public EmployeeRepository(IDbService dbService, ILogger<EmployeeRepository> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        public int AddEmployee(Employee employee)
        {
            int employeeId = 0;
            using (var transactionScope = new TransactionScope())
            {
                if (employee.Department != null)
                {
                    var existingDepartment = GetDepartment(employee.Department);
                    employee.DepartmentId = existingDepartment == null
                        ? CreateDepartment(employee.Department) : existingDepartment.Name == employee.Department.Name
                        ? existingDepartment.Id : throw new AppException("Введенный номер телефона департамента уже существует");
                }
                if (employee.Company != null)
                {
                    var existingCompany = GetCompany(employee.Company);
                    employee.CompanyId = existingCompany == null ? CreateCompany(employee.Company) : existingCompany.Id;
                }

                employeeId = CreateEmployee(employee);

                if (employee.Passports?.Count > 0)
                {
                    List<Passport> existingPassports = GetPassports(employee.Passports);
                    if (existingPassports.Count == 0) CreatePassport(employee.Passports, employeeId);
                    else throw new AppException("Введенный номер паспорта уже существует");
                }
                transactionScope.Complete();
            }
            return employeeId;
        }

        #region Private methods for AddEmployee

        private Company? GetCompany(Company company)
        {
            try
            {
                string getCompany = "SELECT * FROM Company WHERE name = @name";
                return _dbService.GetOne<Company>(getCompany, company).Result;
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is PostgresException pgException)
                {
                    switch (pgException.SqlState)
                    {
                        case "P0002": // data not found
                            _logger.LogInformation("Компания не найдена: {ex}", ex);
                            return null;
                        default:
                            throw;
                    }
                }
                throw;
            }
        }

        private List<Passport> GetPassports(List<Passport> passports)
        {
            try
            {
                string getPassport = "SELECT * FROM Passport WHERE number in (@numbers);";
                return _dbService.GetMany<Passport>(getPassport, new { numbers = passports.Select(x => x.Number + ", ").ToString()}).Result;
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is PostgresException pgException)
                {
                    switch (pgException.SqlState)
                    {
                        case "P0002": // data not found
                            _logger.LogInformation("Паспорт не найден: {ex}", ex);
                            return new List<Passport>();
                        default:
                            throw;
                    }
                }
                throw;
            }
        }

        private List<DbString> GetAnsiStrings(List<Passport> passports)
        {
            var result = new List<DbString>();
            foreach (var passport in passports)
            {
                result.Add(new DbString { IsAnsi = true, Value = passport.Number });
            }
            return result;
        }

        private Department? GetDepartment(Department department)
        {
            try
            {
                string getDepartment = "SELECT * FROM Department WHERE phone = @phone";
                return _dbService.GetOne<Department>(getDepartment, department).Result;
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is PostgresException pgException)
                {
                    switch (pgException.SqlState)
                    {
                        case "P0002": // data not found
                            _logger.LogInformation("Департамент не найден: {ex}", ex);
                            return null;
                        default:
                            throw;
                    }
                }
                throw;
            }
        }

        private int CreateCompany(Company company)
        {
            string createCompany = @"INSERT INTO Company (name) VALUES (@Name);
                                     SELECT currval('company_id_seq');";
            int companyId = _dbService.AddData(createCompany, company).Result;
            return companyId;
        }

        private int CreateDepartment(Department department)
        {
            try
            {
                string createDepartment = @"INSERT INTO Department (name, phone) VALUES (@Name, @Phone);
                                            SELECT currval('department_id_seq');";
                int departmentId = _dbService.AddData(createDepartment, department).Result;
                return departmentId != 0 ? departmentId : throw new Exception("Ошибка при создании департамента");
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is PostgresException pgException)
                {
                    switch (pgException.SqlState)
                    {
                        case "23505": // unique key violation            
                            throw new AppException(ex.Message);
                        default:
                            throw;
                    }
                }
                throw;
            }
        }

        private void CreatePassport(List<Passport> passports, int employeeId)
        {
            try
            {
                using (var transactionScope = new TransactionScope())
                {
                    foreach (var passport in passports)
                    {
                        passport.EmployeeId = employeeId;

                        string createPassport = "INSERT INTO Passport (employee_id, type, number) VALUES (@EmployeeId, @Type, @Number);";
                        int passportId = _dbService.AddData(createPassport, passport).Result;
                    }
                    transactionScope.Complete();
                }
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is PostgresException pgException)
                {
                    switch (pgException.SqlState)
                    {
                        case "23505": // unique key violation            
                            throw new AppException(ex.Message);
                        default:
                            throw;
                    }
                }
                throw;
            }
        }

        private int CreateEmployee(Employee employee)
        {
            try
            {
                string createEmployee = @"INSERT INTO Employee (name, surname, phone, company_id, department_id)
                                          VALUES (@Name, @Surname, @Phone, @CompanyId, @DepartmentId);
                                          SELECT currval('employee_id_seq');";
                return _dbService.AddData(createEmployee, employee).Result;
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is PostgresException pgException)
                {
                    switch (pgException.SqlState)
                    {
                        case "23505": // unique key violation            
                            throw new AppException(ex.Message);
                        default:
                            throw;
                    }
                }
                throw;
            }
        }

        #endregion

        public void DeleteEmployee(int employeeId)
        {
            using (var transactionScope = new TransactionScope())
            {
                _dbService.EditData("DELETE FROM Passport WHERE employee_id = @Id", new { Id = employeeId });
                _dbService.EditData("DELETE FROM Employee WHERE id = @Id", new { Id = employeeId });

                transactionScope.Complete();
            }
        }

        public List<Employee> GetEmployeesByDepartmentId(int id)
        {
            string sql = @"SELECT e.id, e.name, e.surname, e.phone, p.id, p.employee_id, p.type, p.number, d.id, d.name, d.phone, c.id, c.name 
                           FROM Employee e
                           LEFT JOIN Passport p ON e.id = p.employee_id
                           LEFT JOIN Department d ON e.department_id = d.id
                           LEFT JOIN Company c ON e.company_id = c.id
                           WHERE e.department_id = @Id";

            Dictionary<int, Employee> dict = new Dictionary<int, Employee>();

            return _dbService.GetMany(sql,
                new[]
                {
                    typeof(Employee),
                    typeof(Passport),
                    typeof(Department),
                    typeof(Company)
                },
                obj =>
                {
                    return MapEmployee(dict, obj);
                }, new { Id = id }).Result.Distinct().ToList();
        }

        public List<Employee> GetEmployeesByCompanyId(int id)
        {
            string sql = @"SELECT e.id, e.name, e.surname, e.phone, p.id, p.employee_id, p.type, p.number, d.id, d.name, d.phone, c.id, c.name 
                           FROM Employee e
                           LEFT JOIN Passport p ON e.id = p.employee_id
                           LEFT JOIN Department d ON e.department_id = d.id
                           LEFT JOIN Company c ON e.company_id = c.id
                           WHERE e.company_id = @Id";

            Dictionary<int, Employee> dict = new Dictionary<int, Employee>();

            return _dbService.GetMany(sql,
                new[]
                {
                    typeof(Employee),
                    typeof(Passport),
                    typeof(Department),
                    typeof(Company)
                },
                obj => 
                {
                    return MapEmployee(dict, obj);
                }, new { Id = id }).Result.Distinct().ToList();
        }

        #region Private methods for GetEmployees
        private Employee MapEmployee(Dictionary<int, Employee> dict, object[] types)
        {
            Employee employee = types[0] as Employee;
            Passport passport = types[1] as Passport;
            Department department = types[2] as Department;
            Company company = types[3] as Company;

            if (!dict.TryGetValue(employee.Id, out Employee? employeeEntry))
            {
                employeeEntry = employee;
                employeeEntry.Passports = new List<Passport>();
                employeeEntry.Department = department;
                employeeEntry.Company = company;
                dict.Add(employeeEntry.Id, employeeEntry);
            }
            
            if (passport != null)
                employeeEntry.Passports?.Add(passport);

            return employeeEntry;
        }

        #endregion

        public void UpdateEmployeeById(int employeeId, Employee employee)
        {
            using (var transactionScope = new TransactionScope())
            {
                if (employee.Department != null) UpdateDepartment(employee.Department);
                if (employee.Company != null) UpdateCompany(employee.Company);

                UpdateEmployee(employee);

                if (employee.Passports?.Count > 0) UpdatePassport(employee.Passports);

                transactionScope.Complete();
            }
        }

        #region Private methods for UpdateEmployee

        private void UpdateEmployee(Employee employee)
        {
            try
            {
                bool alreadyAdded = false;
                StringBuilder query = new StringBuilder("UPDATE Employee SET");

                if (employee.Name != null)
                {
                    query.Append(" name = @Name");
                    alreadyAdded = true;
                }
                if (employee.Surname != null)
                { 
                    string temp = alreadyAdded ? "," : "";
                    query.Append(temp + " surname = @Surname");
                    alreadyAdded = true;
                }
                if (employee.Phone != null)
                {
                    string temp = alreadyAdded ? "," : "";
                    query.Append(temp + " phone = @Phone");
                    alreadyAdded = true;
                }
                query.Append(" WHERE id = @id;");

                if (alreadyAdded) _dbService.EditData(query.ToString(), employee);
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is PostgresException pgException)
                {
                    switch (pgException.SqlState)
                    {
                        case "23505": // unique key violation            
                            throw new AppException(ex.Message);
                        default:
                            throw;
                    }
                }
                throw;
            }
        }

        private void UpdateDepartment(Department department)
        {
            try 
            {
                bool alreadyAdded = false;
                StringBuilder query = new StringBuilder("UPDATE Department SET");

                if (department.Name != null)
                {
                    query.Append(" name = @Name");
                    alreadyAdded = true;
                }
                if (department.Phone != null)
                {
                    string temp = alreadyAdded ? "," : "";
                    query.Append(temp + " phone = @Phone");
                    alreadyAdded = true;
                }
                query.Append(" WHERE id = @id;");

                if (alreadyAdded) _dbService.EditData(query.ToString(), department);
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is PostgresException pgException)
                {
                    switch (pgException.SqlState)
                    {
                        case "23505": // unique key violation            
                            throw new AppException(ex.Message);
                        default:
                            throw;
                    }
                }
                throw;
            }
        }

        private void UpdatePassport(List<Passport> passports)
        {
            try 
            {
                foreach (Passport passport in passports)
                {
                    bool alreadyAdded = false;
                    StringBuilder query = new StringBuilder("UPDATE Passport SET");

                    if (passport.Type != null)
                    {
                        query.Append(" type = @Type");
                        alreadyAdded = true;
                    }
                    if (passport.Number != null)
                    {
                        string temp = alreadyAdded ? "," : "";
                        query.Append(temp + " number = @Number");
                        alreadyAdded = true;
                    }

                    query.Append(" WHERE id = @id;");

                    if (alreadyAdded) _dbService.EditData(query.ToString(), passport);
                }
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException() is PostgresException pgException)
                {
                    switch (pgException.SqlState)
                    {
                        case "23505": // unique key violation            
                            throw new AppException(ex.Message);
                        default:
                            throw;
                    }
                }
            throw;
            }
        }

        private void UpdateCompany(Company company)
        {
            _dbService.EditData("UPDATE Company SET name = @Name WHERE id = @Id;", company);
        }

        #endregion
    }
}
