using System;
using Dex.DataProvider.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Dex.DataProvider.Ef.Transactions
{
    public sealed class EfDecoratorDataTransaction : IDataTransaction
    {
        private readonly IDataTransaction _dataTransaction;
        private readonly DbContext _dbContext;

        public EfDecoratorDataTransaction(IDataTransaction dataTransaction, DbContext dbContext)
        {
            _dataTransaction = dataTransaction ?? throw new ArgumentNullException(nameof(dataTransaction));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <inheritdoc />
        public void Complete()
        {
            try
            {
                _dataTransaction.Complete();
            }
            catch (Exception)
            {
                _dbContext.ChangeTracker.Clear();
                throw;
            }
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            _dataTransaction.Dispose();
        }
    }
}