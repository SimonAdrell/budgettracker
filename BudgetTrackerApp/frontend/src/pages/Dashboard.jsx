import { useEffect, useRef, useState } from 'react';
import accountService from '../services/accountService';
import dashboardService from '../services/dashboardService';
import './Dashboard.css';

const balanceFormatter = new Intl.NumberFormat(undefined, {
  style: 'currency',
  currency: 'XXX',
  currencyDisplay: 'symbol',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const dateFormatter = new Intl.DateTimeFormat(undefined, {
  year: 'numeric',
  month: 'long',
  day: 'numeric',
});

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
  const hasDashboardData = selectedAccountId && !loadingDashboard && !dashboardError && dashboardData;

  const formatBalance = (amount) => {
    if (amount == null) {
      return balanceFormatter.format(0);
    }

    return balanceFormatter.format(amount);
  };

  const formatLastUpdated = (dateValue) => {
    if (!dateValue) {
      return 'Last updated unavailable';
    }

    const [year, month, day] = dateValue.split('-').map(Number);

    if (!year || !month || !day) {
      return 'Last updated unavailable';
    }

    return `Last updated ${dateFormatter.format(new Date(year, month - 1, day))}`;
  };

  const formatTransactionCount = (count) => {
    if (count === 1) {
      return 'Based on 1 recorded transaction';
    }

    return `Based on ${count ?? 0} recorded transactions`;
  };

  const getBalanceClassName = (amount) => {
    if (amount < 0) {
      return 'dashboard-ledger-hero-balance-negative';
    }

    if (amount > 0) {
      return 'dashboard-ledger-hero-balance-positive';
    }

    return 'dashboard-ledger-hero-balance-neutral';
  };

  return (
    <div className="dashboard-shell">
      <header className="dashboard-panel dashboard-shell-header">
        <p className="dashboard-shell-kicker">Dashboard v1</p>
        <h1>Account dashboard</h1>
        <p className="dashboard-shell-copy">
          Review one account at a time with a clear balance summary based on the
          latest imported ledger activity.
        </p>
      </header>

      <main className="dashboard-shell-grid">
        <section className="dashboard-panel" aria-labelledby="dashboard-account-selector-heading">
          <p className="dashboard-section-label">Account focus</p>
          <h2 id="dashboard-account-selector-heading">Source account</h2>
          <p className="dashboard-section-copy">
            Choose the account whose imported ledger should drive the balance summary.
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
            <div className="dashboard-account-card">
              <div className="dashboard-form-group">
                <label htmlFor="dashboard-account-select">Reporting account</label>
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
                <div className="dashboard-account-card-foot">
                  <p className="dashboard-account-card-note">
                    Balance and activity below reflect {selectedAccount.name}.
                  </p>
                  {selectedAccount.accountNumber && (
                    <p className="dashboard-account-card-detail">
                      Account number ending in {selectedAccount.accountNumber.slice(-4)}
                    </p>
                  )}
                </div>
              )}
            </div>
          )}
        </section>

        <section className="dashboard-panel" aria-labelledby="dashboard-ledger-hero-heading">
          <p className="dashboard-section-label">Ledger summary</p>
          <h2 id="dashboard-ledger-hero-heading">Balance summary</h2>
          <p className="dashboard-section-copy">
            The figure below is the latest recorded balance for the selected account.
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

          {hasDashboardData && (
            <div className="dashboard-ledger-hero" aria-live="polite">
              <div className="dashboard-ledger-hero-header">
                <p className="dashboard-ledger-hero-label">Current balance</p>
                <p className="dashboard-ledger-hero-account">{dashboardData.accountName}</p>
              </div>
              <p className={`dashboard-ledger-hero-balance ${getBalanceClassName(dashboardData.currentBalance)}`}>
                {formatBalance(dashboardData.currentBalance)}
              </p>
              <div className="dashboard-ledger-hero-support">
                <p className="dashboard-ledger-hero-updated">
                  {dashboardData.hasTransactions
                    ? formatLastUpdated(dashboardData.lastUpdated)
                    : 'No transactions imported yet'}
                </p>
                <p className="dashboard-ledger-hero-meta">
                  {formatTransactionCount(dashboardData.transactionCount)}
                </p>
              </div>
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
