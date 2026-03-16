import { useEffect, useRef, useState } from 'react';
import accountService from '../services/accountService';
import dashboardService from '../services/dashboardService';
import './Dashboard.css';

function Dashboard() {
  const [accounts, setAccounts] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState('');
  const [loadingAccounts, setLoadingAccounts] = useState(true);
  const [accountsError, setAccountsError] = useState('');
  const [dashboardData, setDashboardData] = useState(null);
  const [loadingDashboard, setLoadingDashboard] = useState(false);
  const [dashboardError, setDashboardError] = useState('');
  const dashboardRequestSequence = useRef(0);

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

  useEffect(() => {
    const requestId = dashboardRequestSequence.current + 1;
    dashboardRequestSequence.current = requestId;

    if (!selectedAccountId) {
      setDashboardData(null);
      setDashboardError('');
      setLoadingDashboard(false);
      return undefined;
    }

    let isActive = true;

    const loadDashboard = async () => {
      setDashboardData(null);
      setDashboardError('');
      setLoadingDashboard(true);

      try {
        const dashboardPayload = await dashboardService.getAccountDashboard(selectedAccountId);

        if (!isActive || dashboardRequestSequence.current !== requestId) {
          return;
        }

        setDashboardData(dashboardPayload);
        setDashboardError('');
      } catch (error) {
        if (!isActive || dashboardRequestSequence.current !== requestId) {
          return;
        }

        console.error('Error loading dashboard data:', error);
        setDashboardData(null);
        setDashboardError('Failed to load dashboard data for the selected account.');
      } finally {
        if (isActive && dashboardRequestSequence.current === requestId) {
          setLoadingDashboard(false);
        }
      }
    };

    loadDashboard();

    return () => {
      isActive = false;
    };
  }, [selectedAccountId]);

  const selectedAccount = accounts.find(
    (account) => account.id.toString() === selectedAccountId
  );
  const dashboardAccountName = dashboardData?.accountName || selectedAccount?.name || 'selected account';

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
              <div className="dashboard-form-group">
                <label htmlFor="dashboard-account-select">Dashboard account</label>
                <select
                  id="dashboard-account-select"
                  value={selectedAccountId}
                  onChange={(event) => setSelectedAccountId(event.target.value)}
                >
                  {accounts.map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name} {account.accountNumber ? `(${account.accountNumber})` : ''}
                    </option>
                  ))}
                </select>
              </div>
              {selectedAccount && (
                <p className="dashboard-section-copy dashboard-selected-account-copy">
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
            render from page-local dashboard state in the next task.
          </p>
          {!selectedAccountId && (
            <div className="dashboard-placeholder dashboard-placeholder-hero">
              Select an account to load dashboard summary data.
            </div>
          )}

          {selectedAccountId && loadingDashboard && (
            <div className="dashboard-placeholder dashboard-placeholder-hero">
              Loading dashboard summary for {dashboardAccountName}...
            </div>
          )}

          {selectedAccountId && !loadingDashboard && dashboardError && (
            <div className="dashboard-placeholder dashboard-placeholder-hero">
              {dashboardError}
            </div>
          )}

          {selectedAccountId && !loadingDashboard && !dashboardError && dashboardData && (
            <div className="dashboard-placeholder dashboard-placeholder-hero">
              Dashboard data loaded for {dashboardData.accountName}. Balance summary
              fields are ready to render.
            </div>
          )}
        </section>

        <section className="dashboard-panel dashboard-panel-wide" aria-labelledby="dashboard-recent-activity-heading">
          <p className="dashboard-section-label">Recent activity area</p>
          <h2 id="dashboard-recent-activity-heading">Recent transactions</h2>
          <p className="dashboard-section-copy">
            A compact preview list will render from page-local dashboard state in
            the next task.
          </p>
          {!selectedAccountId && (
            <ul className="dashboard-placeholder-list" aria-label="Recent activity placeholder list">
              <li>Select an account to load recent transactions.</li>
            </ul>
          )}

          {selectedAccountId && loadingDashboard && (
            <ul className="dashboard-placeholder-list" aria-label="Recent activity placeholder list">
              <li>Loading recent activity for {dashboardAccountName}...</li>
            </ul>
          )}

          {selectedAccountId && !loadingDashboard && dashboardError && (
            <ul className="dashboard-placeholder-list" aria-label="Recent activity placeholder list">
              <li>{dashboardError}</li>
            </ul>
          )}

          {selectedAccountId && !loadingDashboard && !dashboardError && dashboardData && (
            <ul className="dashboard-placeholder-list" aria-label="Recent activity placeholder list">
              <li>
                {dashboardData.recentTransactions.length} recent transaction item(s)
                are ready for {dashboardData.accountName}.
              </li>
            </ul>
          )}
        </section>
      </main>
    </div>
  );
}

export default Dashboard;
