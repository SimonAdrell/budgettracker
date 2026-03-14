import { useEffect, useState } from 'react';
import accountService from '../services/accountService';
import transactionService from '../services/transactionService';
import './Transactions.css';

const numberFormatter = new Intl.NumberFormat(undefined, {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

function Transactions() {
  const [accounts, setAccounts] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState('');
  const [transactions, setTransactions] = useState([]);
  const [loadingAccounts, setLoadingAccounts] = useState(true);
  const [loadingTransactions, setLoadingTransactions] = useState(false);
  const [accountsError, setAccountsError] = useState('');
  const [transactionsError, setTransactionsError] = useState('');

  const loadAccounts = async () => {
    setLoadingAccounts(true);
    setAccountsError('');

    try {
      const accountsData = await accountService.getAccounts();
      setAccounts(accountsData);
      setSelectedAccountId((currentSelectedAccountId) => {
        const hasCurrentSelection = accountsData.some(
          (account) => account.id.toString() === currentSelectedAccountId
        );

        if (hasCurrentSelection) {
          return currentSelectedAccountId;
        }

        return accountsData[0]?.id.toString() || '';
      });
      setTransactions([]);
    } catch (err) {
      setAccountsError(err.response?.data?.detail || 'Failed to load accounts. Please try again.');
    } finally {
      setLoadingAccounts(false);
    }
  };

  useEffect(() => {
    loadAccounts();
  }, []);

  const loadTransactions = async (accountId) => {
    if (!accountId) {
      setTransactions([]);
      setTransactionsError('');
      return;
    }

    setLoadingTransactions(true);
    setTransactionsError('');

    try {
      const transactionRows = await transactionService.getTransactionsForAccount(accountId);
      setTransactions(transactionRows);
    } catch (err) {
      setTransactions([]);
      setTransactionsError(err.response?.data?.detail || 'Failed to load transactions. Please try again.');
    } finally {
      setLoadingTransactions(false);
    }
  };

  useEffect(() => {
    loadTransactions(selectedAccountId);
  }, [selectedAccountId]);

  const selectedAccount = accounts.find((account) => account.id.toString() === selectedAccountId);

  const formatDate = (dateValue) => {
    if (!dateValue) {
      return '-';
    }

    return dateValue;
  };

  const formatAmount = (amount) => {
    if (amount == null) {
      return '-';
    }

    return numberFormatter.format(amount);
  };

  return (
    <div className="transactions-container">
      <div className="transactions-header">
        <h1>Transactions</h1>
        <p>Browse imported transactions for one account.</p>
      </div>

      <div className="transactions-shell">
        {loadingAccounts && (
          <div className="transactions-state transactions-state-loading">
            Loading accounts...
          </div>
        )}

        {!loadingAccounts && accountsError && (
          <div className="transactions-state transactions-state-error">
            <strong>{accountsError}</strong>
            <button type="button" className="transactions-retry" onClick={loadAccounts}>
              Try Again
            </button>
          </div>
        )}

        {!loadingAccounts && !accountsError && accounts.length === 0 && (
          <div className="transactions-state">
            <h2>No accounts available</h2>
            <p>Create and import into an account first, then come back to browse transactions.</p>
          </div>
        )}

        {!loadingAccounts && !accountsError && accounts.length > 0 && (
          <>
            <div className="transactions-controls">
              <div className="transactions-form-group">
                <label htmlFor="transactions-account-select">Account</label>
                <select
                  id="transactions-account-select"
                  value={selectedAccountId}
                  onChange={(e) => setSelectedAccountId(e.target.value)}
                  disabled={loadingTransactions}
                >
                  {accounts.map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name} {account.accountNumber ? `(${account.accountNumber})` : ''}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {loadingTransactions && (
              <div className="transactions-state transactions-state-loading">
                Loading transactions...
              </div>
            )}

            {!loadingTransactions && transactionsError && (
              <div className="transactions-state transactions-state-error">
                <strong>{transactionsError}</strong>
                <button
                  type="button"
                  className="transactions-retry"
                  onClick={() => loadTransactions(selectedAccountId)}
                >
                  Refresh
                </button>
              </div>
            )}

            {!loadingTransactions && !transactionsError && transactions.length === 0 && (
              <div className="transactions-state">
                <h2>No transactions yet</h2>
                <p>
                  {selectedAccount
                    ? `There are no imported transactions for ${selectedAccount.name} yet.`
                    : 'There are no imported transactions for this account yet.'}
                </p>
              </div>
            )}

            {!loadingTransactions && !transactionsError && transactions.length > 0 && (
              <div className="transactions-table-wrapper">
                <table className="transactions-table">
                  <thead>
                    <tr>
                      <th>Booking Date</th>
                      <th>Transaction Date</th>
                      <th>Description</th>
                      <th>Amount</th>
                      <th>Balance</th>
                    </tr>
                  </thead>
                  <tbody>
                    {transactions.map((transaction) => (
                      <tr key={transaction.id}>
                        <td>{formatDate(transaction.bookingDate)}</td>
                        <td>{formatDate(transaction.transactionDate)}</td>
                        <td>{transaction.description}</td>
                        <td className={transaction.amount < 0 ? 'amount-negative' : 'amount-positive'}>
                          {formatAmount(transaction.amount)}
                        </td>
                        <td>{formatAmount(transaction.balance)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}

export default Transactions;
