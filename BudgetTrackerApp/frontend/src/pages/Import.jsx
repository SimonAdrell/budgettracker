import { useState, useEffect, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import authService from '../services/authService';
import accountService from '../services/accountService';
import importService from '../services/importService';
import './Import.css';

function Import() {
  const location = useLocation();
  const navigate = useNavigate();
  const [accounts, setAccounts] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState('');
  const [file, setFile] = useState(null);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState(null);
  const [showCreateAccount, setShowCreateAccount] = useState(false);
  const [newAccountName, setNewAccountName] = useState('');
  const [newAccountNumber, setNewAccountNumber] = useState('');
  const fileInputRef = useRef(null);
  const preselectedAccountIdRef = useRef(
    location.state?.preselectedAccountId?.toString() || ''
  );
  const cameFromDashboard = location.state?.from === 'dashboard';

  useEffect(() => {
    loadAccounts();
  }, []);

  const loadAccounts = async () => {
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

        const preferredAccountId = preselectedAccountIdRef.current;
        const hasPreferredSelection = accountsData.some(
          (account) => account.id.toString() === preferredAccountId
        );

        if (hasPreferredSelection) {
          return preferredAccountId;
        }

        return '';
      });
    } catch (error) {
      console.error('Error loading accounts:', error);
      setMessage({
        type: 'error',
        text: 'Failed to load accounts. Please try again.',
      });
    }
  };

  const handleLogout = async () => {
    await authService.logout();
    navigate('/login');
  };

  const handleBackToDashboard = () => {
    navigate('/dashboard');
  };

  const handleFileChange = (e) => {
    const selectedFile = e.target.files[0];
    if (selectedFile) {
      const extension = selectedFile.name.split('.').pop().toLowerCase();
      if (extension !== 'xls' && extension !== 'xlsx') {
        setMessage({
          type: 'error',
          text: 'Please select an Excel file (.xls or .xlsx)',
        });
        setFile(null);
        e.target.value = '';
        return;
      }
      setFile(selectedFile);
      setMessage(null);
    }
  };

  const handleCreateAccount = async () => {
    if (!newAccountName.trim()) {
      setMessage({
        type: 'error',
        text: 'Account name is required',
      });
      return;
    }

    setLoading(true);
    try {
      const newAccount = await accountService.createAccount(
        newAccountName,
        newAccountNumber || null
      );
      setAccounts([...accounts, newAccount]);
      setSelectedAccountId(newAccount.id.toString());
      setNewAccountName('');
      setNewAccountNumber('');
      setShowCreateAccount(false);
      setMessage({
        type: 'success',
        text: `Account "${newAccount.name}" created successfully!`,
      });
    } catch (error) {
      console.error('Error creating account:', error);
      setMessage({
        type: 'error',
        text: error.response?.data?.error || 'Failed to create account. Please try again.',
      });
    } finally {
      setLoading(false);
    }
  };

  const handleImport = async () => {
    if (!selectedAccountId) {
      setMessage({
        type: 'error',
        text: 'Please select an account',
      });
      return;
    }

    if (!file) {
      setMessage({
        type: 'error',
        text: 'Please select a file to import',
      });
      return;
    }

    setLoading(true);
    setMessage(null);

    try {
      const result = await importService.uploadTransactions(file, selectedAccountId);
      
      if (result.success) {
        const successMessage = `Successfully imported ${result.importedCount} transaction(s).`;
        const warnings = result.duplicateCount > 0 
          ? `${result.duplicateCount} duplicate(s) were skipped.` 
          : '';
        
        setMessage({
          type: 'success',
          text: successMessage,
          details: warnings ? [warnings] : [],
          warnings: result.warnings || [],
        });
        
        // Reset file input
        setFile(null);
        if (fileInputRef.current) {
          fileInputRef.current.value = '';
        }
      } else {
        setMessage({
          type: 'error',
          text: 'Import failed',
          errors: result.errors || [],
        });
      }
    } catch (error) {
      console.error('Error importing transactions:', error);
      const errorMessage = error.response?.data?.error || 
                          error.response?.data?.errors?.[0] ||
                          'Failed to import transactions. Please check the file format and try again.';
      setMessage({
        type: 'error',
        text: errorMessage,
        errors: error.response?.data?.errors || [],
      });
    } finally {
      setLoading(false);
    }
  };

  const selectedAccount = accounts.find(
    (account) => account.id.toString() === selectedAccountId
  );

  return (
    <div className="import-container">
      <div className="import-header">
        <div>
          <h1>Import Transactions</h1>
          {cameFromDashboard && (
            <p className="import-flow-copy">
              Bring new ledger activity into {selectedAccount?.name || 'your selected account'} and
              return to the dashboard when you are done.
            </p>
          )}
        </div>
        <div className="import-header-actions">
          <button
            type="button"
            onClick={handleBackToDashboard}
            className="button button-secondary import-header-button"
          >
            Back to Dashboard
          </button>
          <button onClick={handleLogout} className="logout-button">
            Logout
          </button>
        </div>
      </div>

      {message && (
        <div className={`alert alert-${message.type}`}>
          <strong>{message.text}</strong>
          {message.details && message.details.length > 0 && (
            <p>{message.details.join(' ')}</p>
          )}
          {message.errors && message.errors.length > 0 && (
            <ul>
              {message.errors.map((error, index) => (
                <li key={index}>{error}</li>
              ))}
            </ul>
          )}
          {message.warnings && message.warnings.length > 0 && (
            <ul>
              {message.warnings.map((warning, index) => (
                <li key={index}>{warning}</li>
              ))}
            </ul>
          )}
          {message.type === 'success' && (
            <div className="import-success-actions">
              <button
                type="button"
                className="button button-secondary"
                onClick={handleBackToDashboard}
              >
                Return to dashboard
              </button>
            </div>
          )}
        </div>
      )}

      <div className="import-section">
        <h2>1. Select or Create Account</h2>
        
        <div className="form-group">
          <label htmlFor="account-select">Select Account:</label>
          <select
            id="account-select"
            value={selectedAccountId}
            onChange={(e) => setSelectedAccountId(e.target.value)}
            disabled={loading}
          >
            <option value="">-- Select an account --</option>
            {accounts.map((account) => (
              <option key={account.id} value={account.id}>
                {account.name} {account.accountNumber ? `(${account.accountNumber})` : ''}
              </option>
            ))}
          </select>
        </div>

        <button
          onClick={() => setShowCreateAccount(!showCreateAccount)}
          className="button button-secondary"
          disabled={loading}
        >
          {showCreateAccount ? 'Cancel' : 'Create New Account'}
        </button>

        {showCreateAccount && (
          <div className="account-creation">
            <h3>Create New Account</h3>
            <div className="form-group">
              <label htmlFor="account-name">Account Name: *</label>
              <input
                type="text"
                id="account-name"
                value={newAccountName}
                onChange={(e) => setNewAccountName(e.target.value)}
                placeholder="e.g., Main Checking Account"
                disabled={loading}
              />
            </div>
            <div className="form-group">
              <label htmlFor="account-number">Account Number (optional):</label>
              <input
                type="text"
                id="account-number"
                value={newAccountNumber}
                onChange={(e) => setNewAccountNumber(e.target.value)}
                placeholder="e.g., 1234-5678"
                disabled={loading}
              />
            </div>
            <button
              onClick={handleCreateAccount}
              className="button button-success"
              disabled={loading || !newAccountName.trim()}
            >
              {loading ? 'Creating...' : 'Create Account'}
            </button>
          </div>
        )}
      </div>

      <div className="import-section">
        <h2>2. Upload Excel File</h2>
        
        <div className="form-group">
          <label htmlFor="file-upload">Choose Excel File:</label>
          <input
            type="file"
            id="file-upload"
            accept=".xls,.xlsx"
            onChange={handleFileChange}
            disabled={loading}
            ref={fileInputRef}
          />
          {file && (
            <div className="file-info">
              Selected: {file.name} ({(file.size / 1024).toFixed(2)} KB)
            </div>
          )}
        </div>

        <div className="button-group">
          <button
            onClick={handleImport}
            className="button button-primary"
            disabled={loading || !selectedAccountId || !file}
          >
            {loading ? 'Importing...' : 'Import Transactions'}
          </button>
        </div>

        <div style={{ marginTop: '20px', fontSize: '14px', color: '#6c757d' }}>
          <p><strong>Expected Excel format:</strong></p>
          <ul>
            <li>Bokföringsdatum (Booking Date)</li>
            <li>Transaktionsdatum (Transaction Date)</li>
            <li>Text (Description)</li>
            <li>Insättning/Uttag (Amount)</li>
            <li>Behållning (Balance)</li>
          </ul>
        </div>
      </div>
    </div>
  );
}

export default Import;
