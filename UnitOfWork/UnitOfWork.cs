using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace UnitOfWork
{
    public class UnitOfWork<TDbContext> : IUnitOfWork where TDbContext : DbContext
    {
        private readonly TDbContext _dbContext;
        private bool _disposed = false;
        protected Dictionary<Type, object> repositories;

        public UnitOfWork(TDbContext context)
        {
            _dbContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets the specified repository for the <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="hasBaseRepository"><c>True</c> if providing custom repositry</param>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <returns>An instance of type inherited from <see cref="IBaseRepository{TEntity}"/> interface.</returns>
        public IBaseRepository<TEntity> GetRepository<TEntity>(bool hasBaseRepository = false) where TEntity : class
        {
            if (repositories == null)
            {
                repositories = new Dictionary<Type, object>();
            }

            if (hasBaseRepository)
            {
                var customRepo = _dbContext.GetService<IBaseRepository<TEntity>>();
                if (customRepo != null)
                {
                    return customRepo;
                }
            }

            var type = typeof(TEntity);
            if (!repositories.ContainsKey(type))
            {
                repositories[type] = new BaseRepository<TEntity>(_dbContext);
            }

            return (IBaseRepository<TEntity>)repositories[type];
        }

        public int SaveChanges()
        {
            return _dbContext.SaveChanges();
        }

        public Task<int> SaveChangesAsync()
        {
            return _dbContext.SaveChangesAsync();
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
                    // clear repositories
                    if (repositories != null)
                    {
                        repositories.Clear();
                    }

                    // dispose the db context.
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


        public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object param = null, IDbContextTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return (await GetConnection().QueryAsync<T>(sql, param, transaction?.GetDbTransaction())).AsList();
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbContextTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await GetConnection().QueryFirstOrDefaultAsync<T>(sql, param, transaction?.GetDbTransaction());
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbContextTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await GetConnection().QuerySingleAsync<T>(sql, param, transaction?.GetDbTransaction());
        }

        public async Task<int> ExecuteAsync(string sql, object param = null, IDbContextTransaction transaction = null, CancellationToken cancellationToken = default)
        {
            return await GetConnection().ExecuteAsync(sql, param, transaction?.GetDbTransaction());
        }
    }
}
