import './Dashboard.css';

function Dashboard() {
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
            Account loading and selection will be added in a later task.
          </p>
          <div className="dashboard-placeholder">Account selector placeholder</div>
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
