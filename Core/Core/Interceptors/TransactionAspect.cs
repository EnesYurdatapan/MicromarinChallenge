using Castle.DynamicProxy;
using System;
using System.Transactions;

public class TransactionAspect : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        using (var transactionScope = new TransactionScope(TransactionScopeOption.Required,
                                                           TransactionScopeAsyncFlowOption.Enabled))
        {
            try
            {
                // Yöntemi çağır
                invocation.Proceed();

                // Eğer hata yoksa transaction'u onayla
                transactionScope.Complete();
            }
            catch (Exception ex)
            {
                // Hata olursa exception fırlat ve transaction'u geri al
                Console.WriteLine($"Transaction failed: {ex.Message}");
                throw;
            }
        }
    }
}
