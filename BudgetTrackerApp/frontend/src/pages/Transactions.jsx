import { useEffect, useState } from 'react';
import accountService from '../services/accountService';
import './Transactions.css';

function Transactions() {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [accountCount, setAccountCount] = useState(0);

  const loadShellState = async () => {
    setLoading(true);
    setError('');

    try {
      const accounts = await accountService.getAccounts();
      setAccountCount(accounts.length);
    } catch (err) {
      setError(err.response?.data?.detail || 'Failed to load transactions page. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadShellState();
  }, []);

  return (
    <div className="transactions-container">
      <div className="transactions-header">
        <h1>Transactions</h1>
        <p>Browse imported transactions for one account.</p>
      </div>

      <div className="transactions-shell">
        {loading && (
          <div className="transactions-state transactions-state-loading">
            Loading transactions page...
          </div>
        )}

        {!loading && error && (
          <div className="transactions-state transactions-state-error">
            <strong>{error}</strong>
            <button type="button" className="transactions-retry" onClick={loadShellState}>
              Try Again
            </button>
          </div>
        )}

        {!loading && !error && (
          <div className="transactions-state">
            <h2>Page shell ready</h2>
            <p>
              {accountCount > 0
                ? `You have ${accountCount} account${accountCount === 1 ? '' : 's'} ready for transaction browsing.`
                : 'You do not have any accounts yet.'}
            </p>
            <p>The account selector and transaction table will be added in Task 16.</p>
          </div>
        )}
      </div>
    </div>
  );
}

export default Transactions;
