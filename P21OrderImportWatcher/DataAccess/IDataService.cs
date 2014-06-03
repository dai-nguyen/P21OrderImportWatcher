/** 
 * This file is part of the P21OrderImportWatcher project.
 * Copyright (c) 2014 Dai Nguyen
 * Author: Dai Nguyen
**/

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace P21OrderImportWatcher.DataAccess
{
    public interface IDataService<T> where T : class
    {
        Task<T> CreateAsync(T entity, CancellationToken token);
        Task<T> UpdateAsync(T entity, CancellationToken token);
        Task<bool> DeleteAsync(T entity, CancellationToken token);
        Task<T> GetAsync(int id, CancellationToken token);
        IQueryable<T> All();
    }
}
