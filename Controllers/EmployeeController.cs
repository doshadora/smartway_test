using Microsoft.AspNetCore.Mvc;
using Smartway.Models;
using Smartway.Repositories;
using Smartway.Helpers;
using System.Net;

namespace Smartway.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class EmployeeController : ControllerBase
    {
        private IEmployeeRepository _employee;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(IEmployeeRepository employee, ILogger<EmployeeController> logger)
        {
            _employee = employee;
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<Employee> GetEmployeesByCompanyId(int companyId)
        {
            var result = _employee.GetEmployeesByCompanyId(companyId);
            if (result.Count == 0)
                throw new KeyNotFoundException("—отрудники дл€ выбранной компании еще не добавлены");
            return result;
        }

        [HttpGet]
        public IEnumerable<Employee> GetEmployeesByDepartmentId(int departmentId)
        {
            var result = _employee.GetEmployeesByDepartmentId(departmentId);
            if (result.Count == 0)
                throw new KeyNotFoundException("—отрудники дл€ выбранного департамента еще не добавлены");
            return result;
        }

        [HttpPost]
        public int CreateNewEmployee(Employee employee)
        {
            return _employee.AddEmployee(employee);
        }

        [HttpPost]
        public void DeleteEmployee(int id)
        {
            _employee.DeleteEmployee(id);
        }

        [HttpPost]
        public void UpdateEmployee(Employee employee)
        {
            if (employee == null) throw new AppException("Ќе переданы параметры дл€ изменени€");
            _employee.UpdateEmployeeById(employee.Id, employee);
        }
    }
}