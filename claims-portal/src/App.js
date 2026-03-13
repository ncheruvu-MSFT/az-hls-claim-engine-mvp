import React, { useState, useEffect } from 'react';
import axios from 'axios';
import BenefitsView from './components/BenefitsView';
import ClaimValidator from './components/ClaimValidator';
import EligibilityChecker from './components/EligibilityChecker';

const API_BASE = 'https://funcclaimstest001ncv.azurewebsites.net/api';

function App() {
  const [activeTab, setActiveTab] = useState('benefits');
  const [plans, setPlans] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchPlans();
  }, []);

  const fetchPlans = async () => {
    try {
      const response = await axios.get(`${API_BASE}/get-plans`);
      setPlans(response.data);
      setLoading(false);
    } catch (error) {
      console.error('Error fetching plans:', error);
      setLoading(false);
    }
  };

  return (
    <div>
      <div className="header">
        <h1>🏥 Claims Portal</h1>
        <p>Healthcare Claims Management System</p>
      </div>

      <div className="container">
        <div className="nav-tabs">
          <button
            className={`nav-tab ${activeTab === 'benefits' ? 'active' : ''}`}
            onClick={() => setActiveTab('benefits')}
          >
            📋 Benefits & Plans
          </button>
          <button
            className={`nav-tab ${activeTab === 'eligibility' ? 'active' : ''}`}
            onClick={() => setActiveTab('eligibility')}
          >
            ✓ Eligibility Check
          </button>
          <button
            className={`nav-tab ${activeTab === 'validate' ? 'active' : ''}`}
            onClick={() => setActiveTab('validate')}
          >
            ⚖️ Validate Claim
          </button>
        </div>

        {loading ? (
          <div className="loading">Loading plans...</div>
        ) : (
          <>
            {activeTab === 'benefits' && <BenefitsView plans={plans} />}
            {activeTab === 'eligibility' && <EligibilityChecker plans={plans} apiBase={API_BASE} />}
            {activeTab === 'validate' && <ClaimValidator plans={plans} apiBase={API_BASE} />}
          </>
        )}
      </div>
    </div>
  );
}

export default App;
