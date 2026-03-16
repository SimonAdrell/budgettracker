import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import accountService from '../services/accountService';
import dashboardService from '../services/dashboardService';
import './Dashboard.css';

const balanceFormatter = new Intl.NumberFormat(undefined, {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

const dateFormatter = new Intl.DateTimeFormat(undefined, {
  year: 'numeric',
  month: 'long',
  day: 'numeric',
});

function Dashboard() {
  const navigate = useNavigate();
  const [accounts, setAccounts] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState('');
  const [loadingAccounts, setLoadingAccounts] = useState(true);
  const [accountsError, setAccountsError] = useState('');
  const [accountsReloadKey, setAccountsReloadKey] = useState(0);
  const [dashboardData, setDashboardData] = useState(null);
  const [loadingDashboard, setLoadingDashboard] = useState(false);
  const [dashboardError, setDashboardError] = useState('');
  const [dashboardReloadKey, setDashboardReloadKey] = useState(0);
  const dashboardRequestSequence = useRef(0);

  useEffect(() => {
    let isMounted = true;

    const loadAccounts = async () => {
      setLoadingAccounts(true);
      setAccountsError('');

      try {
        const accountsData = await accountService.getAccounts();

        if (!isMounted) {
          return;
        }

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
  }, [accountsReloadKey]);

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
  }, [selectedAccountId, dashboardReloadKey]);

  const reloadAccounts = () => {
    setAccountsReloadKey((currentValue) => currentValue + 1);
  };

  const reloadDashboard = () => {
    setDashboardReloadKey((currentValue) => currentValue + 1);
  };

  const selectedAccount = accounts.find(
    (account) => account.id.toString() === selectedAccountId
  );
  const dashboardAccountName = dashboardData?.accountName || selectedAccount?.name || 'selected account';
  const showFirstRunState = !loadingAccounts && !accountsError && accounts.length === 0;
  const hasDashboardData = Boolean(
    selectedAccountId && !loadingDashboard && !dashboardError && dashboardData
  );
  const showNoTransactionsState =
    !showFirstRunState &&
    hasDashboardData &&
    dashboardData.hasTransactions === false;
  const recentTransactions = dashboardData?.recentTransactions?.slice(0, 8) || [];
  const showRunningBalance = recentTransactions.some((transaction) => transaction.balance != null);

  const formatDateValue = (dateValue) => {
    if (!dateValue) {
      return '';
    }

    const [year, month, day] = dateValue.split('-').map(Number);

    if (!year || !month || !day) {
      return '';
    }

    return dateFormatter.format(new Date(year, month - 1, day));
  };

  const formatBalance = (amount) => {
    if (amount == null) {
      return balanceFormatter.format(0);
    }

    return balanceFormatter.format(amount);
  };

  const formatLastUpdated = (dateValue) => {
    const formattedDate = formatDateValue(dateValue);

    if (!formattedDate) {
      return 'Last updated unavailable';
    }

    return `Last updated ${formattedDate}`;
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

  const formatTransactionDate = (dateValue) => {
    return formatDateValue(dateValue) || 'Date unavailable';
  };

  const getTransactionAmountClassName = (amount) => {
    if (amount < 0) {
      return 'dashboard-transaction-amount-negative';
    }

    if (amount > 0) {
      return 'dashboard-transaction-amount-positive';
    }

    return 'dashboard-transaction-amount-neutral';
  };

  const renderAccountLoadingState = () => (
    <div className="dashboard-account-card dashboard-account-card-loading" aria-hidden="true">
      <div className="dashboard-skeleton dashboard-skeleton-label" />
      <div className="dashboard-skeleton dashboard-skeleton-input" />
      <div className="dashboard-account-card-foot">
        <div className="dashboard-skeleton dashboard-skeleton-copy" />
        <div className="dashboard-skeleton dashboard-skeleton-detail" />
      </div>
    </div>
  );

  const renderFeedbackCard = (message, actionLabel, actionHandler, options = {}) => {
    const { hero = false, alert = false } = options;

    return (
      <div
        className={`dashboard-feedback-card${hero ? ' dashboard-feedback-card-hero' : ''}`}
        role={alert ? 'alert' : 'status'}
      >
        <p>{message}</p>
        {actionLabel && actionHandler && (
          <button
            type="button"
            className="dashboard-feedback-action"
            onClick={actionHandler}
          >
            {actionLabel}
          </button>
        )}
      </div>
    );
  };

  const renderHeroSkeleton = () => (
    <div className="dashboard-loading-block" role="status" aria-live="polite">
      <p className="dashboard-loading-copy">
        Loading dashboard summary for {dashboardAccountName}...
      </p>
      <div className="dashboard-ledger-hero dashboard-ledger-hero-skeleton" aria-hidden="true">
        <div className="dashboard-ledger-hero-header">
          <div className="dashboard-skeleton dashboard-skeleton-hero-label" />
          <div className="dashboard-skeleton dashboard-skeleton-hero-account" />
        </div>
        <div className="dashboard-skeleton dashboard-skeleton-hero-balance" />
        <div className="dashboard-ledger-hero-support">
          <div className="dashboard-skeleton dashboard-skeleton-hero-meta" />
          <div className="dashboard-skeleton dashboard-skeleton-hero-detail" />
        </div>
      </div>
    </div>
  );

  const renderRecentTransactionsSkeleton = () => (
    <div className="dashboard-loading-block" role="status" aria-live="polite">
      <p className="dashboard-loading-copy">
        Loading recent activity for {dashboardAccountName}...
      </p>
      <ul className="dashboard-recent-transactions-list" aria-hidden="true">
        {[0, 1, 2].map((item) => (
          <li key={item} className="dashboard-transaction-preview dashboard-transaction-preview-skeleton">
            <div className="dashboard-transaction-preview-main">
              <div className="dashboard-skeleton dashboard-skeleton-transaction-date" />
              <div className="dashboard-skeleton dashboard-skeleton-transaction-description" />
            </div>
            <div className="dashboard-transaction-preview-amounts">
              <div className="dashboard-skeleton dashboard-skeleton-transaction-amount" />
              <div className="dashboard-skeleton dashboard-skeleton-transaction-balance" />
            </div>
          </li>
        ))}
      </ul>
    </div>
  );

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
        {showFirstRunState && (
          <section
            className="dashboard-panel dashboard-panel-wide dashboard-empty-state-panel"
            aria-labelledby="dashboard-first-run-heading"
          >
            <p className="dashboard-section-label">First run</p>
            <h2 id="dashboard-first-run-heading">No accounts yet</h2>
            <p className="dashboard-section-copy">
              Create an account or import your first file to start this dashboard.
            </p>
            <button
              type="button"
              className="dashboard-empty-state-action"
              onClick={() => navigate('/import')}
            >
              Create account or import
            </button>
          </section>
        )}

        {!showFirstRunState && (
          <section className="dashboard-panel" aria-labelledby="dashboard-account-selector-heading">
            <p className="dashboard-section-label">Account focus</p>
            <h2 id="dashboard-account-selector-heading">Source account</h2>
            <p className="dashboard-section-copy">
              Choose the account whose imported ledger should drive the balance summary.
            </p>

            {loadingAccounts && (
              renderAccountLoadingState()
            )}

            {!loadingAccounts && accountsError && (
              renderFeedbackCard(
                accountsError,
                'Retry account load',
                reloadAccounts,
                { alert: true }
              )
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
        )}

        {!showFirstRunState && !showNoTransactionsState && (
          <section className="dashboard-panel" aria-labelledby="dashboard-ledger-hero-heading">
            <p className="dashboard-section-label">Ledger summary</p>
            <h2 id="dashboard-ledger-hero-heading">Balance summary</h2>
            <p className="dashboard-section-copy">
              The figure below is the latest recorded balance for the selected account.
            </p>
            {loadingAccounts && renderHeroSkeleton()}

            {!loadingAccounts && accountsError && (
              renderFeedbackCard(
                'Retry account loading to unlock the balance summary.',
                'Retry account load',
                reloadAccounts
              )
            )}

            {!loadingAccounts && !accountsError && !selectedAccountId && (
              <div className="dashboard-placeholder dashboard-placeholder-hero">
                Select an account to load dashboard summary data.
              </div>
            )}

            {!loadingAccounts && !accountsError && selectedAccountId && loadingDashboard && (
              renderHeroSkeleton()
            )}

            {selectedAccountId && !loadingDashboard && dashboardError && (
              renderFeedbackCard(
                dashboardError,
                'Retry dashboard load',
                reloadDashboard,
                { hero: true, alert: true }
              )
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
                    {formatLastUpdated(dashboardData.lastUpdated)}
                  </p>
                  <p className="dashboard-ledger-hero-meta">
                    {formatTransactionCount(dashboardData.transactionCount)}
                  </p>
                </div>
              </div>
            )}
          </section>
        )}

        {!showFirstRunState && showNoTransactionsState && (
          <section
            className="dashboard-panel dashboard-empty-state-panel"
            aria-labelledby="dashboard-no-transactions-heading"
          >
            <p className="dashboard-section-label">Next step</p>
            <h2 id="dashboard-no-transactions-heading">No transactions yet</h2>
            <p className="dashboard-section-copy">
              {dashboardAccountName} is ready. Import transactions to show its balance and recent activity.
            </p>
            <button
              type="button"
              className="dashboard-empty-state-action"
              onClick={() => navigate('/import')}
            >
              Import transactions
            </button>
          </section>
        )}

        {!showFirstRunState && !showNoTransactionsState && (
          <section className="dashboard-panel dashboard-panel-wide" aria-labelledby="dashboard-recent-activity-heading">
            <p className="dashboard-section-label">Recent activity area</p>
            <h2 id="dashboard-recent-activity-heading">Recent transactions</h2>
            <p className="dashboard-section-copy">
              A quick preview of the latest imported transactions for the selected account.
            </p>
            {loadingAccounts && renderRecentTransactionsSkeleton()}

            {!loadingAccounts && accountsError && (
              renderFeedbackCard(
                'Retry account loading to unlock recent activity.',
                'Retry account load',
                reloadAccounts
              )
            )}

            {!loadingAccounts && !accountsError && !selectedAccountId && (
              <ul className="dashboard-placeholder-list" aria-label="Recent activity placeholder list">
                <li>Select an account to load recent transactions.</li>
              </ul>
            )}

            {!loadingAccounts && !accountsError && selectedAccountId && loadingDashboard && (
              renderRecentTransactionsSkeleton()
            )}

            {selectedAccountId && !loadingDashboard && dashboardError && (
              renderFeedbackCard(
                dashboardError,
                'Retry dashboard load',
                reloadDashboard,
                { alert: true }
              )
            )}

            {selectedAccountId && !loadingDashboard && !dashboardError && dashboardData && (
              <>
                {recentTransactions.length === 0 ? (
                  <div className="dashboard-placeholder dashboard-placeholder-compact">
                    No recent transactions are available for {dashboardData.accountName} yet.
                  </div>
                ) : (
                  <ul className="dashboard-recent-transactions-list" aria-label="Recent transactions preview">
                    {recentTransactions.map((transaction, index) => (
                      <li
                        key={`${transaction.date}-${transaction.description}-${transaction.amount}-${index}`}
                        className="dashboard-transaction-preview"
                      >
                        <div className="dashboard-transaction-preview-main">
                          <p className="dashboard-transaction-date">
                            {formatTransactionDate(transaction.date)}
                          </p>
                          <p className="dashboard-transaction-description">{transaction.description}</p>
                        </div>
                        <div className="dashboard-transaction-preview-amounts">
                          <p className={`dashboard-transaction-amount ${getTransactionAmountClassName(transaction.amount)}`}>
                            {formatBalance(transaction.amount)}
                          </p>
                          {showRunningBalance && transaction.balance != null && (
                            <p className="dashboard-transaction-balance">
                              Balance {formatBalance(transaction.balance)}
                            </p>
                          )}
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
              </>
            )}
          </section>
        )}
      </main>
    </div>
  );
}

export default Dashboard;
