using Dapper;
using Npgsql;
using System.Data;

namespace Smartway.Services
{
    public class PostgreService : IDbService
    {
        private readonly IDbConnection _dbConnection;

        public PostgreService(IConfiguration configuration)
        {
            _dbConnection = new NpgsqlConnection(configuration.GetConnectionString("Employees"));
        }

        public async Task<T?> GetOne<T>(string query, object parameter)
        {
            return await _dbConnection.QuerySingleOrDefaultAsync<T>(query, parameter);
        }

        public async Task<List<T>> GetMany<T>(string query, object parameters)
        {
            return (await _dbConnection.QueryAsync<T>(query, parameters)).ToList();
        }

        public async Task<List<T>> GetMany<T>(string query, Type[] types = null, Func<object[], T> func = null, object? parameter = null, CommandType? commandType = null)
        {
            return (await _dbConnection.QueryAsync<T>(query, types: types, map: func, param: parameter, commandType: commandType)).ToList();
        }

        public async Task<int> AddData(string query, object parameter)
        {
            return await _dbConnection.QuerySingleOrDefaultAsync<int>(query, parameter);
        }

        public void EditData(string query, object parameter)
        {
            _dbConnection.Execute(query, parameter);
        }

        #region Init DB
        public async Task Init()
        {
            await _initDatabase();
            await _initTables();
        }

        private async Task _initDatabase()
        {
            var sql = $"CREATE DATABASE IF NOT EXISTS `{_dbConnection.Database}`;";
            await _dbConnection.ExecuteAsync(sql);
        }

        private async Task _initTables()
        {
            string sql = @"CREATE TABLE IF NOT EXISTS Department (
                Id SERIAL PRIMARY KEY,
                Name VARCHAR(255),
                Phone VARCHAR(20) UNIQUE
            );";
            await _dbConnection.ExecuteAsync(sql);

            sql = @"CREATE TABLE IF NOT EXISTS Company (
                Id SERIAL PRIMARY KEY,
                Name VARCHAR(255)
            );";
            await _dbConnection.ExecuteAsync(sql);

            sql = @"CREATE TABLE IF NOT EXISTS Employee (
                Id SERIAL PRIMARY KEY,
                Name VARCHAR(255),
                Surname VARCHAR(255),
                Phone VARCHAR(20) UNIQUE,
                Company_id INT,
                Department_id INT,
                FOREIGN KEY (Company_id) REFERENCES Company(Id),
                FOREIGN KEY (Department_id) REFERENCES Department(Id)
            );";
            await _dbConnection.ExecuteAsync(sql);

            sql = @"CREATE TABLE IF NOT EXISTS Passport (
                Id SERIAL PRIMARY KEY,
              Employee_id INT,
                Type VARCHAR(50),
                Number VARCHAR(50) UNIQUE,
              FOREIGN KEY (Employee_id) REFERENCES Employee(Id)
            );";
            await _dbConnection.ExecuteAsync(sql);
        }

        #endregion
    }
}
