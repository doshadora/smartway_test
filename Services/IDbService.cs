using Smartway.Models;
using System.Data;

namespace Smartway.Services
{
    public interface IDbService
    {
        public Task<T?> GetOne<T>(string query, object parameter);
        public Task<List<T>> GetMany<T>(string query, object parameters);
        public Task<List<T>> GetMany<T>(string query, Type[]? types = null, Func<object[], T>? func = null, object? parameter = null, CommandType? commandType = null);
        public Task<int> AddData(string query, object parameter);
        public void EditData(string query, object parameter);
    }
}
