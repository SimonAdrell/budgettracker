import { useEffect, useState } from 'react';
import accountService from '../services/accountService';
import './Dashboard.css';

function Dashboard() {
  const [accounts, setAccounts] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState('');
  const [loadingAccounts, setLoadingAccounts] = useState(true);
  const [accountsError, setAccountsError] = useState('');

  useEffect(() => {
    let isMounted = true;

    const loadAccounts = async () => {
      try {
        const accountsData = await accountService.getAccounts();

        if (!isMounted) {
          return;
        }

        setAccounts(accountsData);
        setSelectedAccountId(accountsData[0]?.id.toString() || '');
        setAccountsError('');
      } catch (error) {
        if (!isMounted) {
          return;
        }

        console.error('Error loading dashboard accounts:', error);
        setAccounts([]);
        setSelectedAccountId('');
        setAccountsError('Failed to load accounts for the dashboard.');
      } finally {
        if (isMounted) {
          setLoadingAccounts(false);
        }
      }
    };

    loadAccounts();

    return () => {
      isMounted = false;
    };
  }, []);

  const selectedAccount = accounts.find(
    (account) => account.id.toString() === selectedAccountId
  );

  return (
    <div className="dashboard-shell">
      <header className="dashboard-panel dashboard-shell-header">
        <p className="dashboard-shell-kicker">Dashboard v1</p>
        <h1>Account dashboard</h1>
        <p className="dashboard-shell-copy">
          This placeholder shell reserves the structure for the protected dashboard
          experience without changing the current redirect or data-loading flow.
        </p>
      </header>

      <main className="dashboard-shell-grid">
        <section className="dashboard-panel" aria-labelledby="dashboard-account-selector-heading">
          <p className="dashboard-section-label">Account selector area</p>
          <h2 id="dashboard-account-selector-heading">Selected account</h2>
          <p className="dashboard-section-copy">
            Choose which account this dashboard will use once dashboard data wiring is added.
          </p>

          {loadingAccounts && (
            <div className="dashboard-placeholder">Loading accounts...</div>
          )}

          {!loadingAccounts && accountsError && (
            <div className="dashboard-placeholder">{accountsError}</div>
          )}

          {!loadingAccounts && !accountsError && accounts.length === 0 && (
            <div className="dashboard-placeholder">
              No accounts are available yet. Import transactions or create an account to begin.
            </div>
          )}

          {!loadingAccounts && !accountsError && accounts.length > 0 && (
            <div className="dashboard-placeholder">
              <label htmlFor="dashboard-account-select" className="dashboard-section-label">
                Dashboard account
              </label>
              <select
                id="dashboard-account-select"
                value={selectedAccountId}
                onChange={(event) => setSelectedAccountId(event.target.value)}
                style={{ display: 'block', width: '100%', marginTop: '0.75rem' }}
              >
                {accounts.map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name} {account.accountNumber ? `(${account.accountNumber})` : ''}
                  </option>
                ))}
              </select>
              {selectedAccount && (
                <p className="dashboard-section-copy">
                  {selectedAccount.name} is currently selected for this dashboard view.
                </p>
              )}
            </div>
          )}
        </section>

        <section className="dashboard-panel" aria-labelledby="dashboard-ledger-hero-heading">
          <p className="dashboard-section-label">Ledger hero area</p>
          <h2 id="dashboard-ledger-hero-heading">Balance summary</h2>
          <p className="dashboard-section-copy">
            The main balance, last updated timestamp, and transaction count will
            render here once dashboard data wiring is in place.
          </p>
          <div className="dashboard-placeholder dashboard-placeholder-hero">
            Ledger hero placeholder
          </div>
        </section>

        <section className="dashboard-panel dashboard-panel-wide" aria-labelledby="dashboard-recent-activity-heading">
          <p className="dashboard-section-label">Recent activity area</p>
          <h2 id="dashboard-recent-activity-heading">Recent transactions</h2>
          <p className="dashboard-section-copy">
            A compact preview list will appear here after the account dashboard
            API is wired into this page.
          </p>
          <ul className="dashboard-placeholder-list" aria-label="Recent activity placeholder list">
            <li>Recent activity placeholder</li>
            <li>Recent activity placeholder</li>
            <li>Recent activity placeholder</li>
          </ul>
        </section>
      </main>
    </div>
  );
}

export default Dashboard;
