using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace UnitOfWork
{
    /// <summary>
    /// 事务拦截器
    /// </summary>
    public class TransactionInterceptor : IInterceptor
    {
        /**
         * 参考：老张的哲学，关于事务的一篇文章介绍
         */
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionInterceptor> _logger;

        public TransactionInterceptor(IUnitOfWork unitOfWork, ILogger<TransactionInterceptor> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// 注意：异步方法和同步方法需要分开处理
        /// </summary>
        /// <param name="invocation"></param>
        public void Intercept(IInvocation invocation)
        {
            var method = invocation.MethodInvocationTarget ?? invocation.Method;
            // 如果标记了 [Transaction]
            if (method.GetCustomAttributes(true).FirstOrDefault(x => x.GetType() == typeof(TransactionAttribute)) is TransactionAttribute)
            {
                //执行原有方法之前
                _logger.LogInformation("执行原有方法之前");
                if (!_unitOfWork.IsEnabledTransaction)
                {
                    _unitOfWork.BeginTransaction();
                }
                var trans = _unitOfWork.CurrentTransaction;
                _logger.LogInformation(new EventId(trans.GetHashCode()), "Use Transaction");
                try
                {
                    // 异步拦截处理
                    if (method.ReturnType.IsAsyncType())
                    {
                        var task = method.Invoke(invocation.InvocationTarget, invocation.Arguments) as Task;
                        task.ContinueWith(x => {
                            if (x.Status == TaskStatus.RanToCompletion)
                            {
                                trans.Commit();
                            }
                            else
                            {
                                trans.Rollback();
                            }
                        }).ConfigureAwait(false);
                        invocation.ReturnValue = task;
                    }
                    else // 同步拦截处理
                    {
                        // 执行原有被拦截的方法
                        invocation.Proceed();
                        if (trans != null)
                        {
                            _logger.LogInformation(new EventId(trans.GetHashCode()), "Transaction Commit");
                            trans.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (trans != null)
                    {
                        _logger.LogInformation(new EventId(trans.GetHashCode()), "Transaction Rollback");
                        trans.Rollback();
                    }
                    throw new Exception(ex.InnerException.Message);
                }

                // 执行原有方法之后
                _logger.LogInformation("执行原有方法之后");
            }
            else
            {
                //如果没有标记[Transaction]，直接执行方法
                try
                {
                    if (method.ReturnType.IsAsyncType())
                    {
                        var task = method.Invoke(invocation.InvocationTarget, invocation.Arguments) as Task;
                        invocation.ReturnValue = task;
                    }
                    else
                    {
                        // 执行原有被拦截的方法
                        invocation.Proceed();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.InnerException.Message);
                }
            }
        }
    }
}
