using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace UnitOfWork
{
    public static class ServiceCollectionExtensions
    {
        #region

        /// <summary>
        /// 业务层单个注入事务拦截器
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="services"></param>
        public static void AddProxiedScoped<TInterface, TImplementation>(this IServiceCollection services) where TInterface : class where TImplementation : class, TInterface
        {
            // This registers the underlying class
            services.AddScoped<TImplementation>();
            services.AddScoped(typeof(TInterface), serviceProvider =>
            {
                // Get an instance of the Castle Proxy Generator
                var proxyGenerator = serviceProvider
                    .GetRequiredService<ProxyGenerator>();
                // Have DI build out an instance of the class that has methods
                // you want to cache (this is a normal instance of that class 
                // without caching added)
                var actual = serviceProvider
                    .GetRequiredService<TImplementation>();
                // Find all of the interceptors that have been registered, 
                // including our caching interceptor.  (you might later add a 
                // logging interceptor, etc.)
                var interceptors = serviceProvider
                    .GetServices<IInterceptor>().ToArray();
                // Have Castle Proxy build out a proxy object that implements 
                // your interface, but adds a caching layer on top of the
                // actual implementation of the class.  This proxy object is
                // what will then get injected into the class that has a 
                // dependency on TInterface
                return proxyGenerator.CreateInterfaceProxyWithTarget(
                    typeof(TInterface), actual, interceptors);
            });
        }

        /// <summary>
        /// 添加事务服务，结合AddProxiedScoped()方法使用
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTransactionService(this IServiceCollection services)
        {
            services.AddSingleton(new ProxyGenerator());
            services.AddScoped<IInterceptor, TransactionInterceptor>();

            return services;
        }

        #endregion

        #region

        /// <summary>
        /// 业务层批量注入事务拦截器
        /// 请在需要拦截的业务方法上添加注解[Transaction]
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="serviceAssemblyName">业务层程序集名称</param>
        public static void AddTransactionService(this ContainerBuilder builder, string serviceAssemblyName)
        {
            /// https://www.cnblogs.com/willardzmh/articles/14393701.html
            /// https://www.cnblogs.com/kasnti/p/12244544.html
            /// https://www.cnblogs.com/hezp/p/11434046.html
            /// https://www.cnblogs.com/sheng-jie/p/7416302.html

            //注册拦截器
            builder.RegisterType<TransactionInterceptor>().AsSelf();

            //注册业务层，同时对业务层的方法进行事务拦截
            builder.RegisterAssemblyTypes(Assembly.Load(serviceAssemblyName))
                .AsImplementedInterfaces().InstancePerLifetimeScope()
                .EnableInterfaceInterceptors()//引用Autofac.Extras.DynamicProxy;
                .InterceptedBy(new Type[] { typeof(TransactionInterceptor) });

            //业务层注册拦截器也可以使用[Intercept(typeof(TransactionInterceptor))]加在类上，但是上面的方法比较好，没有侵入性
        }

        #endregion

        #region

        /// <summary>
        /// Registers the unit of work given context as a service in the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TContext">The type of the db context.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The same service collection so that multiple calls can be chained.</returns>
        /// <remarks>
        /// This method only support one db context, if been called more than once, will throw exception.
        /// </remarks>
        public static IServiceCollection AddUnitOfWork<TContext>(this IServiceCollection services) where TContext : DbContext
        {
            //https://github.com/arch/UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork<TContext>>();

            return services;
        }

        /// <summary>
        /// Registers the base repository as a service in the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <typeparam name="TRepository">The type of the base repositry.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <returns>The same service collection so that multiple calls can be chained.</returns>
        public static IServiceCollection AddBaseRepository<TEntity, TRepository>(this IServiceCollection services)
            where TEntity : class
            where TRepository : class, IBaseRepository<TEntity>
        {
            services.AddScoped<IBaseRepository<TEntity>, TRepository>();

            return services;
        }

        #endregion
    }
}
