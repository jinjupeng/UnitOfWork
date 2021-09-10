using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitOfWork
{
	public static class ReflectionExtension
	{
		/// <summary>
		/// 判断方法是异步方法还是同步方法
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsAsyncType(this Type type)
		{
			var awaiter = type.GetMethod("GetAwaiter");
			if (awaiter == null)
				return false;
			var retType = awaiter.ReturnType;
			//.NET Core 1.1及以下版本中没有 GetInterface 方法，为了兼容性使用 GetInterfaces
			if (retType.GetInterfaces().All(i => i.Name != "INotifyCompletion"))
				return false;
			if (retType.GetProperty("IsCompleted") == null)
				return false;
			if (retType.GetMethod("GetResult") == null)
				return false;
			return true;
		}
	}
}
