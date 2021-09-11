using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
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

        #region command sql

        /// <summary>
        /// 查询
        /// 用法:await _unitOfWork.QueryAsync`Demo`("select id,title from post where id = @id", new { id = 1 });
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="param">参数</param>
        /// <param name="trans"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> QueryAsync<TEntity>(string sql, object param = null, IDbContextTransaction trans = null) where TEntity : class;

        /// <summary>
        /// ExecuteAsync
        /// 用法:await _unitOfWork.ExecuteAsync("update post set title =@title where id =@id", new { title = "", id=1 });
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="sql">sql语句</param>
        /// <param name="param">参数</param>
        /// <param name="trans"></param>
        /// <returns></returns>
        Task<int> ExecuteAsync(string sql, object param, IDbContextTransaction trans = null);

        #endregion

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
    }
}
