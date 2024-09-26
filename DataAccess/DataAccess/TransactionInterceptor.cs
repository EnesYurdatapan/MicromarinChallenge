using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public class TransactionInterceptor : IAsyncInterceptor
{
    private readonly Context _context;

    public TransactionInterceptor(Context context)
    {
        _context = context;
    }
    public void Intercept(IInvocation invocation)
    {
        // Senkron metot çağrısını direkt olarak çalıştır
        invocation.Proceed();
    }

    public async Task InterceptAsync(IInvocation invocation)
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                // Asenkron metot çağrısını çalıştır
                await (Task)invocation.ReturnValue;
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // Hata fırlatıyoruz
            }
        }
    }

    public void InterceptSynchronous(IInvocation invocation)
    {
        // Senkron metot çağrısını çalıştır
        invocation.Proceed();
    }

    public async Task<TResult> InterceptAsynchronous<TResult>(IInvocation invocation)
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                // Asenkron metot çağrısını çalıştır ve sonucu döndür
                TResult result = await (Task<TResult>)invocation.ReturnValue;
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // Hata fırlatıyoruz
            }
        }
    }

    public void InterceptAsynchronous(IInvocation invocation)
    {
        throw new NotImplementedException("Bu metot asenkron olmayan işlemler için kullanılabilir. Uygulamanızda özel bir işlem yoksa, bu metodu kullanmayabilirsiniz.");
    }

    void IAsyncInterceptor.InterceptAsynchronous<TResult>(IInvocation invocation)
    {
        throw new NotImplementedException();
    }
}
