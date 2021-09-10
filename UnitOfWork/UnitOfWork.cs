using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace UnitOfWork
{
    public class UnitOfWork<TDbContext> : IUnitOfWork where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private bool _disposed = false;

        public UnitOfWork(TDbContext context)
        {
            _dbContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        public int SaveChanges()
        {
            return _dbContext.SaveChanges();
        }

        public Task<int> SaveChangesAsync()
        {
            return _dbContext.SaveChangesAsync();
        }

        public Task<IEnumerable<TEntity>> QueryAsync<TEntity>(string sql, object param = null, IDbContextTransaction trans = null) where TEntity : class
        {
            var conn = GetConnection();
            var result = conn.QueryAsync<TEntity>(sql, param, trans?.GetDbTransaction());
            return result;
        }

        public async Task<int> ExecuteAsync(string sql, object param, IDbContextTransaction trans = null)
        {
            var conn = GetConnection();
            return await conn.ExecuteAsync(sql, param, trans?.GetDbTransaction());
        }

        /// <summary>
        /// 获取 当前开启的事务
        /// </summary>
        public IDbContextTransaction CurrentTransaction { get; private set; }

        /// <summary>
        /// 获取 事务是否已提交
        /// </summary>
        public bool HasCommitted { get; private set; }

        /// <summary>
        /// 获取 是否已启用事务
        /// </summary>
        public bool IsEnabledTransaction => CurrentTransaction != null;

        public IDbContextTransaction BeginTransaction()
        {
            if (!IsEnabledTransaction)
            {
                CurrentTransaction = _dbContext.Database.BeginTransaction();
            }
            HasCommitted = false;
            return CurrentTransaction;
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void Commit()
        {
            if (IsEnabledTransaction)
            {
                try
                {
                    CurrentTransaction.Commit();
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            HasCommitted = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
                }
            }
            _disposed = true;
        }

        public IDbConnection GetConnection()
        {
            return _dbContext.Database.GetDbConnection();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void Rollback()
        {
            if (IsEnabledTransaction)
            {
                CurrentTransaction.Rollback();
            }
            HasCommitted = true;
        }
    }
}
