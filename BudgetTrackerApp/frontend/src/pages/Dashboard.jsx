import './Dashboard.css';

function Dashboard() {
  return (
    <div className="dashboard-page">
      <header className="dashboard-header">
        <div>
          <p className="dashboard-eyebrow">Dashboard</p>
          <h1>Account overview</h1>
          <p className="dashboard-intro">
            This shell reserves space for the core dashboard sections before data wiring is added.
          </p>
        </div>
      </header>

      <main className="dashboard-layout">
        <section className="dashboard-panel" aria-labelledby="dashboard-account-selector-heading">
          <div className="dashboard-panel-header">
            <h2 id="dashboard-account-selector-heading">Account selector area</h2>
            <span className="dashboard-panel-tag">Placeholder</span>
          </div>
          <label className="dashboard-field-label" htmlFor="dashboard-account-select">
            Account
          </label>
          <select id="dashboard-account-select" disabled defaultValue="">
            <option value="">Account loading will be added in Task 8</option>
          </select>
          <p className="dashboard-panel-copy">
            The selected account controls which summary and recent activity appear on this page.
          </p>
        </section>

        <section className="dashboard-panel dashboard-hero-panel" aria-labelledby="dashboard-ledger-hero-heading">
          <div className="dashboard-panel-header">
            <h2 id="dashboard-ledger-hero-heading">Ledger hero area</h2>
            <span className="dashboard-panel-tag">Placeholder</span>
          </div>
          <div className="dashboard-hero-content">
            <div>
              <p className="dashboard-hero-label">Current balance</p>
              <strong className="dashboard-hero-value">--</strong>
            </div>
            <div className="dashboard-hero-meta">
              <div>
                <span className="dashboard-meta-label">Last updated</span>
                <span className="dashboard-meta-value">Pending dashboard data wiring</span>
              </div>
              <div>
                <span className="dashboard-meta-label">Transaction count</span>
                <span className="dashboard-meta-value">Pending dashboard data wiring</span>
              </div>
            </div>
          </div>
        </section>

        <section className="dashboard-panel" aria-labelledby="dashboard-recent-activity-heading">
          <div className="dashboard-panel-header">
            <h2 id="dashboard-recent-activity-heading">Recent activity area</h2>
            <span className="dashboard-panel-tag">Placeholder</span>
          </div>
          <ul className="dashboard-activity-list" aria-label="Recent activity placeholders">
            <li>Recent transactions will be rendered here in a later task.</li>
            <li>Empty, loading, and error states are intentionally deferred.</li>
            <li>Import and navigation polish remain outside Task 6.</li>
          </ul>
        </section>
      </main>
    </div>
  );
}

export default Dashboard;
