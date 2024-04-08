using Smartway.Models;

namespace Smartway.Repositories
{
    public interface IEmployeeRepository
    {
        int AddEmployee(Employee employee);
        void DeleteEmployee(int employeeId);
        List<Employee> GetEmployeesByDepartmentId(int id);
        List<Employee> GetEmployeesByCompanyId(int id);
        void UpdateEmployeeById(int employeeId, Employee employee);
    }
}
