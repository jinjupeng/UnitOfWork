using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// 获取指定仓储
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="hasBaseRepository">如有自定义仓储设为True</param>
        /// <returns></returns>
        IBaseRepository<TEntity> GetRepository<TEntity>(bool hasBaseRepository = false) where TEntity : class;

        int SaveChanges();

        Task<int> SaveChangesAsync();


        IDbContextTransaction CurrentTransaction { get; }

        /// <summary>
        /// 获取 是否已提交
        /// </summary>
        bool HasCommitted { get; }

        /// <summary>
        /// 获取 是否启用事务
        /// </summary>
        bool IsEnabledTransaction { get; }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns></returns>
        IDbContextTransaction BeginTransaction();

        /// <summary>
        /// 提交当前上下文的事务更改
        /// </summary>
        void Commit();

        /// <summary>
        /// 回滚所有事务
        /// </summary>
        void Rollback();

        /// <summary>
        /// 获取DbConnection
        /// </summary>
        /// <returns></returns>
        IDbConnection GetConnection();



        #region command sql

        Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object param = null, IDbContextTransaction transaction = null, CancellationToken cancellationToken = default);

        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbContextTransaction transaction = null, CancellationToken cancellationToken = default);

        Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbContextTransaction transaction = null, CancellationToken cancellationToken = default);

        Task<int> ExecuteAsync(string sql, object param = null, IDbContextTransaction transaction = null, CancellationToken cancellationToken = default);

        #endregion
    }
}
